from pathlib import Path
import re, shutil
root=Path('C:/UnityProject/NITROZERO')
source=root/'Library/BoostOnMigrationProject'
rx=re.compile(r'guid:\s*([a-f0-9]{32})')
def index(folders):
    result={}
    for folder in folders:
        for meta in folder.rglob('*.meta'):
            m=rx.search(meta.read_text(errors='ignore'))
            if m: result[m.group(1)]=Path(str(meta)[:-5])
    return result
current=index([root/'Assets',root/'Packages',root/'Library/PackageCache'])
saved=index([source/'Assets'])
queue=[root/'Assets/Scenes/boostOn.unity',root/'Assets/04HYUNWOOK/Prefab/UI.prefab']
visited=set(); copied=[]; missing=set()
while queue:
    file=queue.pop()
    if file in visited or not file.is_file(): continue
    visited.add(file)
    if file.suffix.lower() not in {'.unity','.prefab','.asset','.mat','.controller','.overridecontroller','.uxml','.uss'}: continue
    for guid in rx.findall(file.read_text(errors='ignore')):
        if guid.startswith('0000000000000000'): continue
        if guid not in current:
            src=saved.get(guid)
            if src is None: missing.add(guid); continue
            dest=root/src.relative_to(source)
            if dest.exists(): raise RuntimeError('Existing asset has different GUID: '+str(dest))
            dest.parent.mkdir(parents=True,exist_ok=True)
            shutil.copy2(src,dest);shutil.copy2(str(src)+'.meta',str(dest)+'.meta')
            current[guid]=dest;copied.append(str(dest.relative_to(root)))
        queue.append(current[guid])
report='Restored dependencies:\n'+'\n'.join(copied)+'\nUnresolved GUIDs:\n'+'\n'.join(sorted(missing))
(root/'Documentation/BoostOn-RecoveryBackup-20260911-205832/dependency-report.txt').write_text(report)
print(report)
