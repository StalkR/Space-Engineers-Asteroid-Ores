using System.Collections.Generic;
using Torch;
using VRage;
using VRageMath;

namespace StalkR.AsteroidOres
{
    public class Config : ViewModel
    {

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
            private SerializableVector3D _Center = Vector3D.Zero;
            private double _Radius = 0;

            public bool AllOres { get => _AllOres; set => SetValue(ref _AllOres, value); }
            public HashSet<string> Ores { get => _Ores; set => SetValue(ref _Ores, value); }
            public Vector3D Center { get => _Center; set => SetValue(ref _Center, value); }
            public double Radius { get => _Radius; set => SetValue(ref _Radius, value); }
        }
    }
}
