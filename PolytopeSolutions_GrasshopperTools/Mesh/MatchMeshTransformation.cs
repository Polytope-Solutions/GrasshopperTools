using System;
using System.Collections.Generic;
using Rhino.Geometry;
using Grasshopper.Kernel;
using Grasshopper;

namespace PolytopeSolutionsTools {
    public class MatchMeshTransformation : GH_Component {
        /// <summary>
        /// Initializes a new instance of the MatchMeshTransformation class.
        /// </summary>
        public MatchMeshTransformation()
          : base("Match mesh transformation", "MMT",
              "Match a transformation of a set of meshes.",
              "PolytopeSolutions", "Mesh") { }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
            pManager.AddMeshParameter("Source Meshes", "SM", "Source meshes to match.", GH_ParamAccess.list);
            pManager.AddMeshParameter("Target Mesh", "TM", "Target mesh to match.", GH_ParamAccess.item);
        }
        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) {
            pManager.AddTransformParameter("Transformation", "T", "Transformation of the target mesh to the original.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Matched Mesh Index", "MMI", "Index of the source mesh that was matched.", GH_ParamAccess.item);
        }
        /////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA) {
            // Read inputs
            List<Mesh> originalMeshes = new List<Mesh>();
            if (!DA.GetDataList(0, originalMeshes))
                return;
            Mesh targetMesh = null;
            if (!DA.GetData(1, ref targetMesh))
                return;

            int closestMeshIndex = 0;
            Transform transform = Transform.Identity;
            // Find the closest mesh
            bool found = false;
            bool vertexMatch, faceMatch;
            int indexA, indexB, indexC;
            Random rnd = new Random();
            Plane sourcePlane = Plane.WorldXY, targetPlane = Plane.WorldXY;
            foreach (Mesh mesh in originalMeshes) {
                vertexMatch = (mesh.Vertices.Count == targetMesh.Vertices.Count);
                faceMatch = (mesh.Faces.Count == targetMesh.Faces.Count);
                if (vertexMatch && faceMatch) {
                    found = true;
                    indexA = rnd.Next(targetMesh.Vertices.Count);
                    indexB = rnd.Next(targetMesh.Vertices.Count);
                    indexC = rnd.Next(targetMesh.Vertices.Count);
                    sourcePlane = new Plane(mesh.Vertices[indexA],
                        mesh.Vertices[indexB],
                        mesh.Vertices[indexC]);
                    targetPlane = new Plane(targetMesh.Vertices[indexA],
                        targetMesh.Vertices[indexB],
                        targetMesh.Vertices[indexC]);

                    //transform = Transform.Translation(
                    //    targetMesh.Vertices[indexA] - mesh.Vertices[indexA]);
                    transform = Transform.PlaneToPlane(sourcePlane, targetPlane);

                    break;
                }
                closestMeshIndex++;
            }
            // TODO: Add other ways to match meshes
            //if (found) {
            //}

            // Set Outputs
            DA.SetData(0, transform);
            DA.SetData(1, closestMeshIndex);
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
        public override Guid ComponentGuid  => new Guid("4C8CE3F5-67AA-4E08-A14F-894F026E3D66");
    }
}