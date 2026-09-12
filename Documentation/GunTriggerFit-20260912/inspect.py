import bpy,math
from mathutils import Vector,Quaternion
from pathlib import Path
root=Path('C:/UnityProject/NITROZERO/Documentation/GunTriggerFit-20260912')
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(root/'Gun_Editable.before.fbx'))
bpy.ops.wm.save_as_mainfile(filepath=str(root/'inspection.blend'))
for name in ['Body','Trigger']:
 o=bpy.data.objects[name];m=o.data
 ps=sorted([p for p in m.polygons if p.material_index==1],key=lambda p:p.area,reverse=True)
 print(name,'LARGE CAPS')
 for p in ps[:22]:
  pts=[o.matrix_world@m.vertices[i].co for i in p.vertices]
  if name=='Trigger' or min(v.z for v in pts)<.278:
   print(p.index,'area',round(p.area,4),'xyz',[tuple(round(c,5) for c in v) for v in pts])
scene=bpy.context.scene;scene.render.engine='BLENDER_WORKBENCH';scene.display.shading.light='STUDIO';scene.display.shading.color_type='MATERIAL';scene.display.shading.show_cavity=True
for m in bpy.data.materials:m.diffuse_color=(.06,.065,.07,1) if 'Interior' in m.name else (.5,.5,.5,1)
scene.world=bpy.data.worlds.new('World');scene.world.color=(.3,.3,.3)
camdata=bpy.data.cameras.new('Camera');cam=bpy.data.objects.new('Camera',camdata);scene.collection.objects.link(cam)
cam.location=(-.15,-.7,.30);target=Vector((.07,0,.252));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();camdata.type='ORTHO';camdata.ortho_scale=.18;scene.camera=cam
scene.render.resolution_x=1000;scene.render.resolution_y=850;scene.render.resolution_percentage=100
for name,angle in [('rest',0),('pulled',16)]:
 o=bpy.data.objects['Trigger'];o.rotation_mode='QUATERNION';o.rotation_quaternion=Quaternion((0,-1,0),math.radians(angle))@Quaternion((1,0,0),math.pi/2)
 scene.render.filepath=str(root/(name+'.png'));bpy.ops.render.render(write_still=True)
