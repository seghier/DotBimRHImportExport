using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;

namespace export_DOTBIM
{
    public class ExportDotBimCommand : Command
    {
        public ExportDotBimCommand()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static ExportDotBimCommand Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "ExportDotBimCommand";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // Usually commands in export plug-ins are used to modify settings and behavior.
            // The export work itself is performed by the ExportDotBimPlugin class.
            return Result.Success;
        }
    }
}
