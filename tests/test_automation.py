import datetime as dt
import importlib.util
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

spec=importlib.util.spec_from_file_location('automation',Path(__file__).resolve().parents[1]/'scripts/automation.py')
policy=importlib.util.module_from_spec(spec)
spec.loader.exec_module(policy)

class AutomationPolicyTests(unittest.TestCase):
    def test_opt_in_is_required(self):
        self.assertFalse(policy.window('','')['enabled'])
        self.assertFalse(policy.window('false','2026-10-30')['enabled'])

    def test_enabled_configuration_requires_a_valid_date(self):
        with self.assertRaises(ValueError):policy.window('true','')

    def test_bangkok_date_boundary_and_seven_day_cutoff(self):
        utc=dt.timezone.utc
        before=dt.datetime(2026,10,29,16,59,tzinfo=utc)
        first=dt.datetime(2026,10,29,17,0,tzinfo=utc)
        last=dt.datetime(2026,11,5,16,59,tzinfo=utc)
        expired=dt.datetime(2026,11,5,17,0,tzinfo=utc)
        self.assertFalse(policy.window('true','2026-10-30',before)['enabled'])
        self.assertEqual(policy.window('true','2026-10-30',first)['day'],1)
        self.assertEqual(policy.window('true','2026-10-30',last)['day'],7)
        self.assertFalse(policy.window('true','2026-10-30',expired)['enabled'])

    def test_agent_cannot_change_workflows_policy_or_add_escape_paths(self):
        for path in ['.github/workflows/ci.yml','scripts/automation.py','tests/test_automation.py',
                     'AGENTS.md','Web/AGENTS.override.md','Web/../AGENTS.md','/tmp/code','Web/.git/config']:
            self.assertFalse(policy.allowed(path),path)
        for path in ['Web/style.css','Assets/Scripts/Gameplay/LaserEngine.cs','tests/engine.test.cjs','docs/findings.md','package-lock.json']:
            self.assertTrue(policy.allowed(path),path)

class PatchIntegrationTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root=Path(self.temp.name)
        self.repo=self.root/'repo'
        self.repo.mkdir()
        self.git('init','-q')
        self.git('config','user.name','Test')
        self.git('config','user.email','test@example.invalid')
        (self.repo/'Web').mkdir()
        (self.repo/'Web/fixture.txt').write_text('baseline\n')
        self.git('add','.')
        self.git('commit','-qm','baseline')
        self.patch=self.root/'candidate.patch'
        self.helper=Path(policy.__file__).resolve()

    def git(self,*args):
        return subprocess.check_output(['git',*args],cwd=self.repo)

    def export_and_apply(self):
        subprocess.run([sys.executable,str(self.helper),'export','--patch',str(self.patch)],cwd=self.repo,check=True,capture_output=True)
        self.git('reset','--hard','-q','HEAD')
        return subprocess.run([sys.executable,str(self.helper),'apply','--patch',str(self.patch)],cwd=self.repo,capture_output=True,text=True)

    def test_real_patch_roundtrip_includes_new_files(self):
        (self.repo/'Web/fixture.txt').write_text('improved\n')
        (self.repo/'Web/new.txt').write_text('new asset\n')
        result=self.export_and_apply()
        self.assertEqual(result.returncode,0,result.stderr)
        self.assertEqual((self.repo/'Web/new.txt').read_text(),'new asset\n')
        self.assertIn('Web/new.txt',self.git('diff','--cached','--name-only').decode())

    def test_fresh_apply_rejects_workflow_change(self):
        folder=self.repo/'.github/workflows'
        folder.mkdir(parents=True)
        (folder/'unsafe.yml').write_text('name: forbidden\n')
        self.git('add','-A')
        self.patch.write_bytes(self.git('diff','--cached','--binary'))
        self.git('reset','--hard','-q','HEAD')
        result=subprocess.run([sys.executable,str(self.helper),'apply','--patch',str(self.patch)],cwd=self.repo,capture_output=True,text=True)
        self.assertNotEqual(result.returncode,0)
        self.assertIn('rejects path',result.stderr)

    def test_symlink_export_is_rejected(self):
        (self.repo/'Web/linked.txt').symlink_to('/etc/passwd')
        result=subprocess.run([sys.executable,str(self.helper),'export','--patch',str(self.patch)],cwd=self.repo,capture_output=True,text=True)
        self.assertNotEqual(result.returncode,0)
        self.assertIn('Symlinks',result.stderr)

    def test_no_change_exports_an_empty_patch(self):
        result=self.export_and_apply()
        self.assertEqual(result.returncode,0,result.stderr)
        self.assertEqual(self.patch.stat().st_size,0)

if __name__=='__main__':unittest.main()
