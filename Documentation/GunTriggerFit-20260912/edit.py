import bpy,bmesh,math
from mathutils import Vector,Quaternion
from pathlib import Path
root=Path('C:/UnityProject/NITROZERO/Documentation/GunTriggerFit-20260912')
bpy.ops.wm.open_mainfile(filepath=str(root/'inspection.blend'))
body=bpy.data.objects['Body']; trigger=bpy.data.objects['Trigger']
# A localized clearance pocket removes inward protrusions without cutting outer cheeks.
bpy.ops.mesh.primitive_cube_add(size=1,location=(.0815,0,.258))
cutter=bpy.context.object;cutter.name='TemporaryTriggerClearance';cutter.dimensions=(.053,.038,.026)
bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
bpy.context.view_layer.objects.active=body
mod=body.modifiers.new('Trim trigger pocket','BOOLEAN');mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=cutter
bpy.ops.object.modifier_apply(modifier=mod.name);bpy.data.objects.remove(cutter,do_unlink=True)
# Extend only the top cut boundary into the receiver. The pivot and visible finger face stay fixed.
inv=trigger.matrix_world.inverted();extended=0
for v in trigger.data.vertices:
 p=trigger.matrix_world@v.co
 if abs(p.z-.271)<.000002:
  p.z=.285;v.co=inv@p;extended+=1
trigger.data.update()
# Trim the jagged rear edge of the moving trigger to a closed, straight cut.
bpy.ops.mesh.primitive_cube_add(size=1,location=(.125,0,.251))
cutter=bpy.context.object;cutter.dimensions=(.05,.08,.045)
bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
bpy.context.view_layer.objects.active=trigger
mod=trigger.modifiers.new('Clean rear trigger edge','BOOLEAN');mod.operation='DIFFERENCE';mod.solver='EXACT';mod.object=cutter
bpy.ops.object.modifier_apply(modifier=mod.name);bpy.data.objects.remove(cutter,do_unlink=True)
# Rounded, recessed receiver backing closes the joint throughout the pull.
bm=bmesh.new();bm.from_mesh(body.data);inv=body.matrix_world.inverted()
loops=[]
for y in [-.010,.010]:
 loops.append([bm.verts.new(inv@Vector((.115+.017*math.cos(i*math.tau/48),y,.265+.017*math.sin(i*math.tau/48)))) for i in range(48)])
faces=[bm.faces.new(loops[0]),bm.faces.new(list(reversed(loops[1])))]
for i in range(48):
 j=(i+1)%48;faces.append(bm.faces.new([loops[0][i],loops[1][i],loops[1][j],loops[0][j]]))
for f in faces:f.material_index=1
for f in faces[2:]:f.smooth=True
bmesh.ops.recalc_face_normals(bm,faces=faces);bm.to_mesh(body.data);bm.free();body.data.update()
print('EXTENDED_TRIGGER_ROOT_VERTICES',extended)
bpy.ops.wm.save_as_mainfile(filepath=str(root/'edited.blend'))
exec(open(str(root/'preview.py')).read())


