using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace JustWithJoints.Core
{
    public class Motion
    {
        public List<Pose> Poses { get; set; }

        public Motion()
        {
            Poses = new List<Pose>();
        }

        public void AddPose(Pose pose)
        {
            if (pose == null)
            {
                return;
            }
            Poses.Add(pose);
        }
    }
}