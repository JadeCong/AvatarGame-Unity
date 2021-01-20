BVH Exporter manual

*Overview
BVH Exporter output animation data (. Bvh) from the motion of the character.


*Installation
Add the C#script  BVHBase.cs , BVHExAll.cs, BVHExNormal.cs, the BVHExMecanim.cs. as asset.
In UNITY version 3 or less, you can not use Mecanim.
In this case, just add BVHBase.cs and BVHExNormal.cs

* How to use
If you want to output all node,
drag and drop  BVHExAll.cs to the root of the character.
(In this case, detail can be output. ie. finger)

If you use a character that is configured with Mecanim,
drag and drop BVHExMecanim.cs. to the root of the character.

If you use a character that has no Mecanim, 
drag and drop BVHExNormal.cs. to the root of the character.

BVHExNormal.cs and  BVHExMecanim.cs are almost same.
But If you use BVHExNormal. , you need to set each node manually.

In the folder that is set in the field of Folder Name, 
it will be saved automatically in a file named. Bvh Take ~.


* Description of each parameter
-Folder Name :It is used for specify the folder name.
 Default is "document folder".


*Right Hand Coordinate
When output as right-handed coordinate system, check this parameter.
 If it is no checked , it will  be Left-handed coordinate system.


*Original Name
It will output in the name of the intended node that is specified in the UNITY.
If it is not checked, the output will use a fixed name. (Hips, Chest, and such as RightHand)


*FromOrigin
If you want to record  around the origin, check this parameter.
If it is not checked, the point that you started capture  becomes the center.


*Scale
This parameter changes the size of the charactor. 


*Capture Mode
If you select "FromStartToFinish" ,it will be recorded from the beginning to the end of the running
If you select "ShortcutKey", you can record using the shortcut key.
If you select "RecordButton", button for capture will appear.
Default is "FromStartToFinish".


*Shortcut Key
If ShortcutKey mode selected, You can specify any key to start or exit.


*Node  (Hips, Chest, Right Hand, etc.) *Only BVHExNormal 
You need to set the  each node .
You must  set  "Hips"  always.


* Notes
When you start running, the charactor must be in a neutral state, such as T-pose.
Do not change parameters while  it is running.
The script using Mecanim, the error  message  will be  displayed if  required node does not have settings.
But there is no problem with data output as long as you have properly configured node.

*Tutorial video
<a href="http://www.youtube.com/watch?v=D3bWK1hrKa4">http://www.youtube.com/watch?v=D3bWK1hrKa4</a>
(subtitle English/Japanese)<br><br>

