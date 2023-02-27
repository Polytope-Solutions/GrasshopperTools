using System;
using System.Collections.Generic;
using Rhino.Geometry;
using Grasshopper.Kernel;
using Grasshopper;

using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PolytopeSolutionsTools {
    public class WritePickAndPlaceDataComponent : GH_Component {
        public string targetFileNameSaved;
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public WritePickAndPlaceDataComponent()
          : base("WritePickAndPlaceDataComponent", "WPPDC",
              "Write a yaml file for pick and place",
              "PolytopeSolutions", "IAAC") { }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddTextParameter("FolderPath", "FP", "Path containing yaml and ply mesh files.", GH_ParamAccess.item, string.Empty);
            pManager.AddPlaneParameter("Place Planes", "PPs", "Planes for placing of the shards", GH_ParamAccess.list);
            pManager.AddBooleanParameter("Write button", "WB", "A button to write data", GH_ParamAccess.item, false);
        }
        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddTextParameter("Saved Yaml File", "SYF", "Path containing yaml and ply mesh files.", GH_ParamAccess.item);
        }
        /////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA) {
            // Read inputs
            string workPath = string.Empty;
            List<Rhino.Geometry.Plane> placePlanes = new List<Rhino.Geometry.Plane>();
            bool writeButton = false;
            if (!DA.GetData(0, ref workPath) || string.IsNullOrEmpty(workPath)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Work path was not given.");
                return;
            }
            if (!DA.GetDataList(1, placePlanes)) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Place Planes were not given.");
                return;
            }

            // Read original yaml file.
            var deserializer = new DeserializerBuilder().Build();
            string fileName = Path.Combine(workPath, "log.yaml");
            dynamic rawData;
            try {
                rawData = deserializer.Deserialize<dynamic>(File.ReadAllText(fileName));
            } catch {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "File not found: " + fileName);
                return;
            }

            // Check if number of shards is the same as number of place planes.
            int.TryParse(rawData["shards"]["num_shards"], out int numShards);
            if (numShards != placePlanes.Count) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Number of place planes does not match number of shards.");
                return;
            }
            
            // Check if button is set to true.
            if (!DA.GetData(2, ref writeButton) || !writeButton) { 
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Click the button to write data.");
            }
            else { 
                // Prepare place planes for the writing.
                for (int i = 0; i < placePlanes.Count; i++) {
                    Rhino.Geometry.Plane plane = placePlanes[i];
                    Rhino.Geometry.Point3d planeOrigin = plane.Origin;
                    rawData["shards"]["shard_" + i]["place"] =  new Dictionary<string, object>();
                    Dictionary<string, double> positionRaw = new Dictionary<string, double>();
                    positionRaw.Add("x", planeOrigin.X);
                    positionRaw.Add("y", planeOrigin.Y);
                    positionRaw.Add("z", planeOrigin.Z);
                    rawData["shards"]["shard_" + i]["place"].Add("position", positionRaw);
                    Dictionary<string, double> quaternionRaw = new Dictionary<string, double>();
                    Rhino.Geometry.Quaternion quaternion = Rhino.Geometry.Quaternion.Rotation(Rhino.Geometry.Plane.WorldXY, placePlanes[i]);
                    quaternionRaw.Add("w", quaternion.A);
                    quaternionRaw.Add("x", quaternion.B);
                    quaternionRaw.Add("y", quaternion.C);
                    quaternionRaw.Add("z", quaternion.D);
                    rawData["shards"]["shard_" + i]["place"].Add("quaternion", quaternionRaw);
                }
                // Write the file.
                string targetFileName = Path.Combine(workPath, "log.yaml");
                try {
                    using (var fileWriter = new StreamWriter(targetFileName)) {
                        var serializer = new Serializer();
                        fileWriter.Write(serializer.Serialize(rawData));
                    }
                }
                catch (Exception ex) {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Could not write file: " + targetFileName + ": " + ex.ToString());
                }
                this.targetFileNameSaved = targetFileName;
            }
            // Set Outputs
            DA.SetData(0, this.targetFileNameSaved);
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
        public override Guid ComponentGuid => new Guid("D4CD12C5-2B46-4954-8E57-B05EDAA4BD8B");
    }
}