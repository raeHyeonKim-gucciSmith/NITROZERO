scene=bpy.context.scene;scene.render.engine='BLENDER_WORKBENCH';scene.display.shading.light='STUDIO';scene.display.shading.color_type='MATERIAL';scene.display.shading.show_cavity=True
for m in bpy.data.materials:m.diffuse_color=(.06,.065,.07,1) if 'Interior' in m.name else (.5,.5,.5,1)
scene.world=bpy.data.worlds.new('World');scene.world.color=(.3,.3,.3)
camdata=bpy.data.cameras.new('Camera');cam=bpy.data.objects.new('Camera',camdata);scene.collection.objects.link(cam)
cam.location=(-.15,-.7,.30);target=Vector((.07,0,.252));cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler();camdata.type='ORTHO';camdata.ortho_scale=.18;scene.camera=cam
scene.render.resolution_x=1000;scene.render.resolution_y=850;scene.render.resolution_percentage=100
for name,angle in [('after_rest',0),('after_half',8),('after_pulled',16)]:
 o=bpy.data.objects['Trigger'];o.rotation_mode='QUATERNION';o.rotation_quaternion=Quaternion((0,-1,0),math.radians(angle))@Quaternion((1,0,0),math.pi/2)
 scene.render.filepath=str(root/(name+'.png'));bpy.ops.render.render(write_still=True)

