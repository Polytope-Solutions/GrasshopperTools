using System;
using System.Collections.Generic;
using Rhino.Geometry;
using Grasshopper.Kernel;
using Grasshopper;

using Aspose.ThreeD;
using Aspose.ThreeD.Entities;

namespace PolytopeSolutionsTools {
    public class ReadMesh : GH_Component {
        /// <summary>
        /// Initializes a new instance of the ReadMesh class.
        /// </summary>
        public ReadMesh()
          : base("Read a mesh from a generic mesh file.", "RM",
              "Read mesh from a file",
              "PolytopeSolutions", "Mesh") { }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddTextParameter("Mesh File Path", "MFP", "Path containing a mesh files.", GH_ParamAccess.item);
        }
        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddMeshParameter("Mesh", "M", "Mesh", GH_ParamAccess.item);
        }
        /////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA) {
            // Read inputs
            string fileName = string.Empty;
            if (!DA.GetData(0, ref fileName) || string.IsNullOrEmpty(fileName)){
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "File path was not given.");
                return;
            }
            // Try to read mesh data and convert to Rhino Geometry.
            Rhino.Geometry.Mesh mesh = null;
            try {
                mesh = ReadMeshFile(fileName);
            }
            catch (Exception ex) {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Failed to read mesh file: " + fileName + ": " + ex.ToString());
            }
            // Set Outputs
            DA.SetData(0, mesh);
        }

        private Rhino.Geometry.Mesh ReadMeshFile(string filePath) {
            Rhino.Geometry.Mesh mesh = new Rhino.Geometry.Mesh();

            // initialize Scene class object
            Scene scene = new Scene();
            // load 3D model
            scene.Open(filePath);
            scene.RootNode.Accept(delegate (Node node) {
                foreach (Entity entity in node.Entities) {
                    // only convert meshes, lights/camera and other stuff will be ignored
                    if (!(entity is IMeshConvertible))
                        continue;
                    Aspose.ThreeD.Entities.Mesh m = ((IMeshConvertible)entity).ToMesh();
                    var controlPoints = m.ControlPoints;
                    // triangulate the mesh, so triFaces will only store triangle indices
                    int[][] triFaces = PolygonModifier.Triangulate(controlPoints, m.Polygons);
                    // try to extract colors if present
                    var vertexColorsElement = (VertexElementVertexColor)m.GetElement(VertexElementType.VertexColor);
                    bool colorDataPresent = (vertexColorsElement != null) && (vertexColorsElement.Data.Count == controlPoints.Count);
                    // control points
                    for (int i = 0; i < controlPoints.Count; i++) {
                        mesh.Vertices.Add(controlPoints[i].x, controlPoints[i].y, controlPoints[i].z);
                        if (colorDataPresent)
                            mesh.VertexColors.Add(
                                (int)(vertexColorsElement.Data[i].x * 255), 
                                (int)(vertexColorsElement.Data[i].y * 255), 
                                (int)(vertexColorsElement.Data[i].z * 255));
                    }
                    // triangle indices
                    for (int i = 0; i < triFaces.Length; i++) {
                        mesh.Faces.AddFace(triFaces[i][0], triFaces[i][1], triFaces[i][2]);
                    }
                }
                return true;
            });

            mesh.Normals.ComputeNormals();
            mesh.Compact();
            return mesh;
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
        public override Guid ComponentGuid => new Guid("73ECB652-B660-4CFD-A3C0-968AE8574F0C");
    }
}