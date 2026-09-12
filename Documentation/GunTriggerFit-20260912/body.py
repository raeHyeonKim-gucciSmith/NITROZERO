import bpy,math
from mathutils import Vector,Quaternion
from pathlib import Path
root=Path('C:/UnityProject/NITROZERO/Documentation/GunTriggerFit-20260912')
bpy.ops.wm.open_mainfile(filepath=str(root/'inspection.blend'))
scene=bpy.context.scene;scene.render.engine='BLENDER_WORKBENCH';scene.display.shading.light='STUDIO';scene.display.shading.color_type='MATERIAL';scene.display.shading.show_cavity=True
for m in bpy.data.materials:m.diffuse_color=(.06,.065,.07,1) if 'Interior' in m.name else (.5,.5,.5,1)
scene.world=bpy.data.worlds.new('World');scene.world.color=(.3,.3,.3)
camdata=bpy.data.cameras.new('Camera');cam=bpy.data.objects.new('Camera',camdata);scene.collection.objects.link(cam)
cam.location=(-.15,-.7,.30);target=Vector((.07,0,.252));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();camdata.type='ORTHO';camdata.ortho_scale=.18;scene.camera=cam
scene.render.resolution_x=1000;scene.render.resolution_y=850;scene.render.resolution_percentage=100

bpy.data.objects['Trigger'].hide_render=True
scene.render.filepath=str(root/'body_only.png');bpy.ops.render.render(write_still=True)
o=bpy.data.objects['Body'];m=o.data
ps=[]
for p in m.polygons:
 if p.material_index!=1:continue
 pts=[o.matrix_world@m.vertices[i].co for i in p.vertices]
 if any(.04<v.x<.115 and .245<v.z<.274 for v in pts):ps.append(p)
for p in sorted(ps,key=lambda p:p.area,reverse=True)[:15]:print(p.index,round(p.area,4),[tuple(round(c,5) for c in o.matrix_world@m.vertices[i].co) for i in p.vertices])

