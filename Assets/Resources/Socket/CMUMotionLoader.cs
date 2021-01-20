using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Reflection;


namespace JustWithJoints
{
    public class CMUMotionLoader
    {
        public static Core.Motion Load(string dataPath, Core.CoordinateSystemType coordinateSystem = Core.CoordinateSystemType.RightHanded)
        {
            Core.Motion motion = new Core.Motion();

            int readPoseCount = 0;
            using (var reader = new System.IO.StreamReader(dataPath))
            {
                Dictionary<string, Vector3> Js = new Dictionary<string, Vector3>();
                int currentFrame = 0;

                while (true)
                {
                    string line = reader.ReadLine();
                    if (line == null)
                    {
                        break;
                    }

                    int frame = parseLine(line, Js);
                    if (frame >= 0)
                    {
                        var pose = ToPose(currentFrame, Js, coordinateSystem);
                        if (pose != null)
                        {
                            while (motion.Poses.Count < currentFrame)
                            {
                                motion.AddPose(pose);
                            }
                            readPoseCount++;
                        }
                        Js.Clear();
                        currentFrame = frame;
                    }
                }

                var finalPose = ToPose(currentFrame, Js, coordinateSystem);
                if (finalPose != null)
                {
                    while (motion.Poses.Count < currentFrame)
                    {
                        motion.AddPose(finalPose);
                    }
                    readPoseCount++;
                }

                Debug.LogFormat("read {0} poses | max frame no = {1}", readPoseCount, motion.Poses.Count);

                return motion;
            }
        }




		public static Core.Motion LoadSocketData(List<string> recvData,int currentFrame, Core.CoordinateSystemType coordinateSystem = Core.CoordinateSystemType.RightHanded)
		{
			Core.Motion motion = new Core.Motion();

			int readPoseCount = 0;
		
			Dictionary<string, Vector3> Js = new Dictionary<string, Vector3>();


			foreach (var line in recvData)
			{
				
				//Debug.Log ("!!!!!!"+line);

				int frame = parseLine(line, Js);
				if (frame >= 0)
				{
					var pose = ToPose(currentFrame, Js, coordinateSystem);
					if (pose != null)
					{
						motion.AddPose(pose);
						readPoseCount++;
					}
					Js.Clear();
				}
			}

			var finalPose = ToPose(currentFrame, Js, coordinateSystem);
			if (finalPose != null)
			{
	
				motion.AddPose(finalPose);
				readPoseCount++;
			}

			if(motion.Poses.Count > 100)
			{

				motion.Poses.RemoveRange(0,50);
				Debug.LogFormat("read {0} poses | max frame no = {1}", readPoseCount, motion.Poses.Count);

			}


			return motion;
			
		}


        // Return a frame number if a new frame starts.
        // Otherwise, return -1
        static int parseLine(string line, Dictionary<string, Vector3> Js)
        {
            line = line.Trim();
            var tokens = line.Split(' ').Select(t => t.Trim()).ToList();
            if (tokens.Count <= 0)
            {
                return -1;
            }
			//ToLower 将字母变成小写
            switch (tokens[0].ToLower())
            {
                case "frame:":
                    int frame;
                    if (int.TryParse(tokens[1], out frame))
                    {
                        return frame;
                    }
                    break;
                default:
                    if (tokens.Count == 4)
                    {
                        float x, y, z;
                        if (float.TryParse(tokens[1], out x) &&
                            float.TryParse(tokens[2], out y) &&
                            float.TryParse(tokens[3], out z))
                        {
                            Js[tokens[0].Trim(':')] = new Vector3(x, y, z);
                        }
                    }
                    return -1;
            }

            return -1;
        }

        static Core.Pose ToPose(int frame, Dictionary<string, Vector3> Js, Core.CoordinateSystemType coordinateSystem)
        {
            string[] bones = {
                "rtibia",
                "rfemur",
                "rhipjoint",
                "lhipjoint",
                "lfemur",
                "ltibia", //5
                "rwrist",
                "rhumerus",
                "rclavicle",
                "lclavicle",
                "lhumerus",
                "lwrist",
                "thorax",
                "head",
            };

            var positions = new List<Vector3>();
            for (int i = 0; i < bones.Length; i++)
            {
                if (Js.ContainsKey(bones[i]) == false)
                {
                    return null;
                }
                var p = Js[bones[i]] * 0.056444f;
                positions.Add(p);
            }
            if (coordinateSystem == Core.CoordinateSystemType.RightHanded)
            {
                // Convert to left-handed because Core.Pose() treats input joint locations as left-handed 
                for (int i = 0; i < positions.Count; i++)
                {
                    var p = positions[i];
                    p.x = -p.x;
                    positions[i] = p;
                }
            }

            Core.Pose pose = new Core.Pose(frame, positions);

            return pose;
        }
    }

}
