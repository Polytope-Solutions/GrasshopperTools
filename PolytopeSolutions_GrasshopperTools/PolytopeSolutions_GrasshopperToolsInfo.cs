using Grasshopper;
using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace PolytopeSolutionsTools {
    public class PolytopeSolutions_GrasshopperToolsInfo : GH_AssemblyInfo {
        public override string Name => "PolytopeSolutionsTools";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => null;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "A set of grasshopper components for various projects.";

        public override Guid Id => new Guid("93590612-b2f0-41e2-b2ea-16154519aff2");

        //Return a string identifying you or your company.
        public override string AuthorName => "Daniil Koshelyuk";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "daniil.koshelyuk@gmail.com";
    }
}