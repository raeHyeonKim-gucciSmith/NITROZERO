import bpy,math,numpy as np
from pathlib import Path
from mathutils import Vector,Quaternion
root=Path('C:/UnityProject/NITROZERO/Documentation/GunTriggerFit-20260912')
bpy.ops.wm.open_mainfile(filepath=str(root/'edited.blend'))
assert set(o.name for o in bpy.context.scene.objects)=={'Body','Slide','Trigger','Muzzle'}
for name in ['Body','Trigger']:
 o=bpy.data.objects[name]
 assert len(o.data.uv_layers)==1
 assert len(o.data.materials)==2
for o in bpy.context.scene.objects:o.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(root/'Gun_Editable.fitted.fbx'),use_selection=True,object_types={'MESH','EMPTY'},axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_ALL',use_mesh_modifiers=False,bake_anim=False,add_leaf_bones=False,path_mode='AUTO')
# Preview from the other side at full pull.
exec(open(str(root/'preview.py')).read().split('for name,angle')[0])
cam.location=(-.15,.7,.30);cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
o=bpy.data.objects['Trigger'];o.rotation_mode='QUATERNION';o.rotation_quaternion=Quaternion((0,-1,0),math.radians(16))@Quaternion((1,0,0),math.pi/2)
scene.render.filepath=str(root/'after_opposite.png');bpy.ops.render.render(write_still=True)
print('EXPORTED_WITH_ORIGINAL_PART_NAMES_AND_UVS')
