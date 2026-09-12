import bpy,json,numpy as np
from pathlib import Path
root=Path('C:/UnityProject/NITROZERO/Documentation/GunTriggerFit-20260912')
bpy.ops.wm.open_mainfile(filepath=str(root/'inspection.blend'))
def snapshot():
 d={}
 for o in bpy.context.scene.objects:
  if o.type not in ['MESH','EMPTY']:continue
  info={'matrix':np.array(o.matrix_world),'type':o.type}
  if o.type=='MESH':
   v=np.empty(len(o.data.vertices)*3);o.data.vertices.foreach_get('co',v);v=v.reshape(-1,3)
   mat=np.array(o.matrix_world);v=v@mat[:3,:3].T+mat[:3,3]
   info.update(vertices=v,uvs=len(o.data.uv_layers),materials=[s.material.name for s in o.material_slots])
  d[o.name]=info
 return d
before=snapshot();bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(root/'Gun_Editable.fitted.fbx'));after=snapshot()
assert set(before)==set(after),(set(before),set(after))
for name,a in before.items():
 b=after[name]
 assert np.allclose(a['matrix'],b['matrix'],atol=1e-5),name+' transform changed'
 if a['type']=='MESH':
  assert a['uvs']==b['uvs'] and a['materials']==b['materials'],name+' UV/material mismatch'
  if name not in ['Body','Trigger']:assert np.allclose(a['vertices'],b['vertices'],atol=1e-6),name+' geometry changed'
  print(name,'vertices',len(a['vertices']),len(b['vertices']),'UV layers',b['uvs'])

print('PASS: part names, transforms, UV layers, materials and untouched Slide/Muzzle preserved')

