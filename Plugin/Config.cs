﻿using System.Collections.Generic;
using Torch;
using VRage;
using VRageMath;

namespace StalkR.AsteroidOres
{
    public class Config : ViewModel
    {
        private bool _Debug = false;
        public bool Debug { get => _Debug; set => SetValue(ref _Debug, value); }

        private bool _AllOres = true;
        private HashSet<string> _Ores = new HashSet<string>();
        private HashSet<Zone> _Zones = new HashSet<Zone>();

        public bool AllOres { get => _AllOres; set => SetValue(ref _AllOres, value); }
        public HashSet<string> Ores { get => _Ores; set => SetValue(ref _Ores, value); }
        public HashSet<Zone> Zones { get => _Zones; set => SetValue(ref _Zones, value); }

        public class Zone : ViewModel
        {
            private bool _AllOres = true;
            private HashSet<string> _Ores = new HashSet<string>();

            // for a spherical cluster
            private SerializableVector3D _Center = Vector3D.Zero;
            private double _MaxRadius = 0;

            // for an asteroid ring, intersect 2 spheres + 2 planes to end up with a washer shape
            // a CloudLayer with RotationAxis 0/0/0 would have its asteroid ring on the Y plane (default)
            private double _MinRadius = 0;
            private double _Height = 0; // total height; only checked if non-zero
            private string _HeightPlane = "Y";

            public bool AllOres { get => _AllOres; set => SetValue(ref _AllOres, value); }
            public HashSet<string> Ores { get => _Ores; set => SetValue(ref _Ores, value); }
            public Vector3D Center { get => _Center; set => SetValue(ref _Center, value); }
            public double MaxRadius { get => _MaxRadius; set => SetValue(ref _MaxRadius, value); }
            public double MinRadius { get => _MinRadius; set => SetValue(ref _MinRadius, value); }
            public double Height { get => _Height; set => SetValue(ref _Height, value); }
            public string HeightPlane { get => _HeightPlane; set => SetValue(ref _HeightPlane, value); }

            internal bool Contains(Vector3D position)
            {
                var d2 = Vector3D.DistanceSquared(position, this.Center);
                if ((d2 > this.MaxRadius * this.MaxRadius)  || (d2 < this.MinRadius * this.MinRadius)) return false;
                if (this.Height == 0) return true;
                double hp = 0;
                switch (this.HeightPlane)
                {
                    case "X": hp = this.Center.X - position.X; break;
                    case "Y": hp = this.Center.Y - position.Y; break;
                    case "Z": hp = this.Center.Z - position.Z; break;
                }
                if ((hp > this.Height / 2) || (hp < -this.Height / 2)) return false;
                return true;
            }
        }
    }
}
