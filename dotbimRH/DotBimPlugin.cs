using Rhino;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace import_DOTBIM
{
    ///<summary>
    /// <para>Every RhinoCommon .rhp assembly must have one and only one PlugIn-derived
    /// class. DO NOT create instances of this class yourself. It is the
    /// responsibility of Rhino to create an instance of this class.</para>
    /// <para>To complete plug-in information, please also see all PlugInDescription
    /// attributes in AssemblyInfo.cs (you might need to click "Project" ->
    /// "Show All Files" to see it in the "Solution Explorer" window).</para>
    ///</summary>
    public class DotBimPlugin : Rhino.PlugIns.FileImportPlugIn
    {
        public DotBimPlugin()
        {
            Instance = this;
        }

        ///<summary>Gets the only instance of the DotBimPlugin plug-in.</summary>
        public static DotBimPlugin Instance { get; private set; }

        ///<summary>Defines file extensions that this import plug-in is designed to read.</summary>
        /// <param name="options">Options that specify how to read files.</param>
        /// <returns>A list of file types that can be imported.</returns>
        protected override Rhino.PlugIns.FileTypeList AddFileTypes(Rhino.FileIO.FileReadOptions options)
        {
            var result = new Rhino.PlugIns.FileTypeList();
            result.AddFileType("DotBim (*.bim)", "bim");
            return result;
        }

        /// <summary>
        /// Is called when a user requests to import a .bim file.
        /// It is actually up to this method to read the file itself.
        /// </summary>
        /// <param name="filename">The complete path to the new file.</param>
        /// <param name="index">The index of the file type as it had been specified by the AddFileTypes method.</param>
        /// <param name="doc">The document to be written.</param>
        /// <param name="options">Options that specify how to write file.</param>
        /// <returns>A value that defines success or a specific failure.</returns>
        protected override bool ReadFile(string filename, int index, RhinoDoc doc, Rhino.FileIO.FileReadOptions options)
        {
            bool read_success = false;
            // TODO: Add code for reading file

            try
            {
                // Use the dotbim library to open and process the BIM file
                var model = dotbim.File.Read(filename);
                var rhinoGeometries = Tools.ConvertBimMeshesAndElementsIntoRhinoMeshes(model.Meshes, model.Elements);
                var fileinfo = model.Info;
                var filekeys = fileinfo.Keys.ToList();
                var filevalues = fileinfo.Values.ToList();

                foreach (var geo in rhinoGeometries)
                {
                    if (geo != null)
                    {
                        int id = rhinoGeometries.IndexOf(geo);
                        var element = model.Elements[id];
                        var minfo = element.Info;
                        var attributes = new Rhino.DocObjects.ObjectAttributes();

                        string mguid = element.Guid;

                        var eColor = element.Color;

                        string mmshid = element.MeshId.ToString();
                        string mrot = Math.Round(element.Rotation.Qw, 3).ToString() + ", " +
                                        Math.Round(element.Rotation.Qx, 3).ToString() + ", " +
                                        Math.Round(element.Rotation.Qy, 3).ToString() + ", " +
                                        Math.Round(element.Rotation.Qz, 3).ToString();
                        string mvect = Math.Round(element.Vector.X, 3).ToString() + ", " +
                                       Math.Round(element.Vector.Y, 3).ToString() + ", " +
                                       Math.Round(element.Vector.Z, 3).ToString();
                        string mtype = element.Type;
                        string mcolor = eColor.A.ToString() + ", " +
                                        eColor.R.ToString() + ", " +
                                        eColor.G.ToString() + ", " +
                                        eColor.B.ToString();

                        // assign atributes to the meshes
                        // to use in Rhino
                        
                        foreach (var fkey in filekeys)
                        {
                            int fid = filekeys.IndexOf(fkey);
                            //attributes.SetUserString("File Info: " + fkey, filevalues[fid]);
                            geo.SetUserString("File Info: " + fkey, filevalues[fid]);
                        }

                        //attributes.SetUserString("Mesh ID", mmshid);
                        //attributes.SetUserString("Rotation", mrot);
                        //attributes.SetUserString("Vector", mvect);
                        //attributes.SetUserString("Color", mcolor);

                        attributes.SetUserString("Guid", mguid);
                        attributes.SetUserString("Type", mtype);
                        attributes.ObjectId = Guid.Parse(mguid);
                        attributes.ObjectColor = System.Drawing.Color.FromArgb(255, eColor.R, eColor.G, eColor.B);
                        attributes.ColorSource = Rhino.DocObjects.ObjectColorSource.ColorFromObject;

                        foreach (var kvp in minfo)
                        {
                            attributes.SetUserString(kvp.Key, kvp.Value);
                            geo.SetUserString("Info: " + kvp.Key, kvp.Value);
                        }

                        // to use in Grasshopper
                        geo.SetUserString("Guid", mguid);
                        geo.SetUserString("Mesh ID", mmshid);
                        geo.SetUserString("Rotation", mrot);
                        geo.SetUserString("Vector", mvect);
                        geo.SetUserString("Type", mtype);
                        geo.SetUserString("Color", mcolor);
                        geo.Compact();

                        doc.Objects.AddMesh(geo, attributes, null, false, false);
                    }
                }
                // Redraw the viewport to display the newly added geometry

                doc.Views.Redraw();

                read_success = true;

            }
            catch (Exception ex)
            {
                string error = Rhino.PlugIns.WriteFileResult.Failure.ToString();
                RhinoApp.WriteLine($"Error opening BIM file {error}: {ex.Message}");
            }

            return read_success;
        }
        // You can override methods here to change the plug-in behavior on
        // loading and shut down, add options pages to the Rhino _Option command
        // and maintain plug-in wide options in a document.
    }
}