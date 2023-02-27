using System;
using System.Collections.Generic;
using Rhino.Geometry;
using Grasshopper.Kernel;
using Grasshopper;

using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Runtime.InteropServices;

namespace PolytopeSolutionsTools {
    public class ReadScanDataComponent : GH_Component {
        /// <summary>
        /// Initializes a new instance of the ReadScanDataComponent class.
        /// </summary>
        public ReadScanDataComponent()
          : base("ReadScanData", "RSD",
            "Read information from YAML file and corresponding PLY mesh data.",
            "PolytopeSolutions", "IAAC") { }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddTextParameter("FolderPath", "FP", "Path containing yaml and ply mesh files.", GH_ParamAccess.item, string.Empty);
            //pManager[0].Optional = true;
        }
        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddPlaneParameter("GroundPlane", "GP", "GroundPlane", GH_ParamAccess.item);
            pManager.AddPlaneParameter("ShardPlanes", "SPs", "ShardPlanes", GH_ParamAccess.list);
            pManager.AddPlaneParameter("ShardPickPlanes", "SPPs", "ShardPickPlanes", GH_ParamAccess.list);
            pManager.AddTextParameter("ShardMeshePaths", "SMPs", "ShardMeshPaths", GH_ParamAccess.list);
            //pManager.HideParameter(0);
        }
        /////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA) {
            // Read inputs
            string workPath = string.Empty;

            if (!DA.GetData(0, ref workPath) || string.IsNullOrEmpty(workPath)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Work path was not given.");
                return;
            }

            // Read original yaml file.
            var deserializer = new YamlDotNet.Serialization.DeserializerBuilder().Build();
            string fileName = Path.Combine(workPath, "log.yaml");
            dynamic rawData;
            try {
                rawData = deserializer.Deserialize<dynamic>(File.ReadAllText(fileName));
            } catch (Exception ex)  {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "File not found: " + fileName + ": " + ex.ToString());
                return;
            }

            // Read the ground Plane.
            double.TryParse(rawData["shards"]["ground_plane"]["a"], out double ga);
            double.TryParse(rawData["shards"]["ground_plane"]["b"], out double gb);
            double.TryParse(rawData["shards"]["ground_plane"]["c"], out double gc);
            double.TryParse(rawData["shards"]["ground_plane"]["d"], out double gd);
            Rhino.Geometry.Plane groundPlane = new Rhino.Geometry.Plane(ga, gb, gc, gd);

            // Read the shards' data.
            int.TryParse(rawData["shards"]["num_shards"], out int numShards);
            Rhino.Geometry.Plane[] shardPlanes = new Rhino.Geometry.Plane[numShards];
            Rhino.Geometry.Plane[] shardPickPlanes = new Rhino.Geometry.Plane[numShards];
            string[] shardMeshFilePaths = new string[numShards];
            for (int i = 0; i < numShards; i++) {
                dynamic shardRaw = rawData["shards"]["shard_" + i.ToString()];
                double.TryParse(shardRaw["plane"]["a"], out double sa);
                double.TryParse(shardRaw["plane"]["b"], out double sb);
                double.TryParse(shardRaw["plane"]["c"], out double sc);
                double.TryParse(shardRaw["plane"]["d"], out double sd);
                Rhino.Geometry.Plane shardPlane = new Rhino.Geometry.Plane(sa, sb, sc, sd);
                shardPlanes[i] = shardPlane;

                double.TryParse(shardRaw["pick"]["position"]["x"], out double spx);
                double.TryParse(shardRaw["pick"]["position"]["y"], out double spy);
                double.TryParse(shardRaw["pick"]["position"]["z"], out double spz);

                double.TryParse(shardRaw["pick"]["quaternion"]["x"], out double sqx);
                double.TryParse(shardRaw["pick"]["quaternion"]["y"], out double sqy);
                double.TryParse(shardRaw["pick"]["quaternion"]["z"], out double sqz);
                double.TryParse(shardRaw["pick"]["quaternion"]["w"], out double sqw);
                Rhino.Geometry.Plane shardPickPlane = Rhino.Geometry.Plane.WorldXY;
                Point3d rpy = QuaternionToEulerAngles(new Rhino.Geometry.Quaternion(sqw, sqx, sqy, sqz));
                Rhino.Geometry.Transform t = Rhino.Geometry.Transform.RotationZYX(rpy.Z, rpy.Y, rpy.X);
                shardPickPlane.Transform(t);
                t = Rhino.Geometry.Transform.Translation(spx, spy, spz);
                shardPickPlane.Transform(t);
                shardPickPlanes[i] = shardPickPlane;

                string shardFileName = Path.Combine(workPath, shardRaw["mesh_path"]);
                shardMeshFilePaths[i] = shardFileName;
            }
            // Set Outputs
            DA.SetData(0, groundPlane);
            DA.SetDataList(1, shardPlanes);
            DA.SetDataList(2, shardPickPlanes);
            DA.SetDataList(3, shardMeshFilePaths);
        }

        private Point3d QuaternionToEulerAngles(Rhino.Geometry.Quaternion q) {
            // this implementation assumes normalized quaternion
            // converts to Euler angles in 3-2-1 sequence
            Point3d angles = new Point3d();

            // roll (x-axis rotation)
            double sinr_cosp = 2 * (q.A * q.B + q.C * q.D);
            double cosr_cosp = 1 - 2 * (q.B * q.B + q.C * q.C);
            angles.X = Math.Atan2(sinr_cosp, cosr_cosp);

            // pitch (y-axis rotation)
            double sinp = Math.Sqrt(1 + 2 * (q.A * q.C - q.B * q.D));
            double cosp = Math.Sqrt(1 - 2 * (q.A * q.C - q.B * q.D));
            angles.Y = 2 * Math.Atan2(sinp, cosp) - Math.PI / 2;

            // yaw (z-axis rotation)
            double siny_cosp = 2 * (q.A * q.D + q.B * q.C);
            double cosy_cosp = 1 - 2 * (q.C * q.C + q.D * q.D);
            angles.Z = Math.Atan2(siny_cosp, cosy_cosp);

            return angles;
        }
        /////////////////////////////////////////////////////////////////////////////////
        public override GH_Exposure Exposure => GH_Exposure.primary;
        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon => null;//Resources.IconForThisComponent;
        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid => new Guid("bb853d1b-fc03-49e1-ab18-4076b8815b0c");
    }
}