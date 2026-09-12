import bpy,json
from mathutils import Vector
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath='C:/UnityProject/NITROZERO/Documentation/GunBodyTrim-20260912/Gun_Editable.original.fbx')
o=bpy.data.objects['Body']; m=o.data
for mi in [0,1]:
 ids={i for p in m.polygons if p.material_index==mi for i in p.vertices}
 pts=[o.matrix_world@m.vertices[i].co for i in ids]
 print('PROFILE',mi)
 for n in range(34):
  x=-.34+n*.02
  ps=[p.z for p in pts if x<=p.x<x+.02]
  print(round(x,3),round(max(ps),5) if ps else None)
print('OBJECTS',[(o.name,o.type,tuple(o.rotation_euler)) for o in bpy.context.scene.objects])
bpy.ops.wm.save_as_mainfile(filepath='C:/UnityProject/NITROZERO/Documentation/GunBodyTrim-20260912/inspection.blend')
