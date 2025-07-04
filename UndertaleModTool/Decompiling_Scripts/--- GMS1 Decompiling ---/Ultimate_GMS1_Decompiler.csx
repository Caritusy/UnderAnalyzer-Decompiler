/*
    Ultimate_GMS1_Decompiler.csx
        (Previously Export2GMS1FIXED_UA)

    Improved
        by burnedpopcorn180

    Original Script by cubeww
    Original Fixed Version by CST1229
		
    Ultimate_GMS1_Decompiler Changes:
        - UI has been added, with the ability to select specific resource types to decompile
        - Added support for decompiling Shaders
        - Added ability to log scripts and objects that failed to decompile to a text file
*/

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Reflection;
// UI
using System.Drawing;
using System.Windows.Forms;
// Basic UTMT stuff
using UndertaleModLib.Models;
using UndertaleModLib.Util;
// Added for UnderAnalyzer Support
using Underanalyzer.Decompiler;
using Underanalyzer;
using UndertaleModTool;

// make sure a data.win is loaded
EnsureDataLoaded();

string GameName = Data.GeneralInfo.Name.ToString().Replace(@"""", ""); //Name == "Project" -> Project
int progress = 0;
string projFolder = GetFolder(FilePath) + GameName + ".gmx" + Path.DirectorySeparatorChar;
TextureWorker worker = new TextureWorker();
string gmxDeclaration = "This Document is generated by GameMaker, if you edit it by hand then you do so at your own risk!";
string eol = "\n"; // Linux: "\n", Windows: "\r\n";

// for error log
List<string> errLog = new List<string>();

// UnderAnalyzer Variable Definitions
GlobalDecompileContext decompileContext = new(Data);
Underanalyzer.Decompiler.IDecompileSettings decompilerSettings = Data.ToolInfo.DecompilerSettings;

#region --------------- UI --------------------------

bool DUMP;
bool OBJT, ROOM, SCPT, TMLN, SOND, SHDR, PATH, FONT, SPRT, BGND;

var tooltip = new ToolTip();

// Setup Style
Form FORM = new Form()
{
    AutoSize = true,
    Text = "Ultimate_GMS1_Decompiler",
    MaximizeBox = false,
    MinimizeBox = false,
    StartPosition = FormStartPosition.CenterScreen,
    FormBorderStyle = FormBorderStyle.FixedDialog,
};
FORM.Controls.Add(new Label()
{
    Text = "Welcome to Ultimate_GMS1_Decompiler!\n\nSelect the parts you want to be included in the project, or just press \"Start Export\" to do a full Export",
    AutoSize = true,
    Location = new System.Drawing.Point(8, 8),
});
FORM.Controls.Add(new Label()
{
    Text = "Select Resource Types to Dump:",
    AutoSize = true,
    Location = new System.Drawing.Point(8, 72),
    Font = new Font(Label.DefaultFont, FontStyle.Bold)
});

#region CheckBox Placement
var _OBJT = new CheckBox()
{
    Location = new System.Drawing.Point(16 + (120 * 0), 100 + (32 * 0)),
    Text = "Objects",
    Checked = true
};
FORM.Controls.Add(_OBJT);

var _ROOM = new CheckBox()
{
    Location = new System.Drawing.Point(16 + (120 * 1), 100 + (32 * 0)),
    Text = "Rooms",
    Checked = true
};
FORM.Controls.Add(_ROOM);

var _SCPT = new CheckBox()
{
    Location = new System.Drawing.Point(16 + (120 * 2), 100 + (32 * 0)),
    Text = "Scripts",
    Checked = true
};
FORM.Controls.Add(_SCPT);

var _TMLN = new CheckBox()
{
    Location = new System.Drawing.Point(16 + (120 * 3), 100 + (32 * 0)),
    Text = "Timelines",
    Checked = true
};
FORM.Controls.Add(_TMLN);

var _SOND = new CheckBox()
{
    Location = new System.Drawing.Point(16 + (120 * 4), 100 + (32 * 0)),
    Text = "Sounds",
    Checked = true
};
FORM.Controls.Add(_SOND);

var _SHDR = new CheckBox()
{
    Location = new System.Drawing.Point(16 + (120 * 5), 100 + (32 * 0)),
    Text = "Shaders",
    Checked = true
};
FORM.Controls.Add(_SHDR);

var _PATH = new CheckBox()
{
    Location = new System.Drawing.Point(16 + (120 * 0), 100 + (32 * 1)),
    Text = "Paths",
    Checked = true
};
FORM.Controls.Add(_PATH);

var _FONT = new CheckBox()
{
    Location = new System.Drawing.Point(16 + (120 * 1), 100 + (32 * 1)),
    Text = "Fonts",
    Checked = true
};
FORM.Controls.Add(_FONT);

var _SPRT = new CheckBox()
{
    Location = new System.Drawing.Point(16 + (120 * 2), 100 + (32 * 1)),
    Text = "Sprites",
    Checked = true
};
FORM.Controls.Add(_SPRT);

var _BGND = new CheckBox()
{
    Location = new System.Drawing.Point(16 + (120 * 3), 100 + (32 * 1)),
    Text = ("Backgrounds"),
    Checked = true
};
FORM.Controls.Add(_BGND);
#endregion
#region Check Resources
// Blank out any Resource Types if there are none of that type in the data.win
if (Data.Sprites.Count == 0)
{
    _SPRT.Checked = false;
    _SPRT.Enabled = false;
}
if (Data.Backgrounds.Count == 0)
{
    _BGND.Checked = false;
    _BGND.Enabled = false;
}
if (Data.GameObjects.Count == 0)
{
    _OBJT.Checked = false;
    _OBJT.Enabled = false;
}
if (Data.Rooms.Count == 0)
{
    _ROOM.Checked = false;
    _ROOM.Enabled = false;
}
if (Data.Sounds.Count == 0)
{
    _SOND.Checked = false;
    _SOND.Enabled = false;
}
if (Data.Scripts.Count == 0)
{
    _SCPT.Checked = false;
    _SCPT.Enabled = false;
}
if (Data.Fonts.Count == 0)
{
    _FONT.Checked = false;
    _FONT.Enabled = false;
}
if (Data.Paths.Count == 0)
{
    _PATH.Checked = false;
    _PATH.Enabled = false;
}
if (Data.Timelines.Count == 0)
{
    _TMLN.Checked = false;
    _TMLN.Enabled = false;
}
if (Data.Shaders.Count == 0)
{
    _SHDR.Checked = false;
    _SHDR.Enabled = false;
}
#endregion

// Export Button
var OKBT = new Button()
{
    Text = (Directory.Exists(projFolder) ? "Overwrite Export" : "Start Export"),
    Dock = DockStyle.Bottom,
    Height = 48,
};
OKBT.Click += (o, s) =>
{
    FORM.Enabled = false;

    // If YYC
    if (Data.IsYYC() && (_SCPT.Checked || _OBJT.Checked))
    {
        if (!ScriptQuestion("This game is YYC compiled, so the actual code is NOT available\n\nAre you SURE that you want to continue?"))
        {
            FORM.Close();
            return;
        }
    }
    // If the Game is in GMS2
    else if (Data.IsGameMaker2())
    {
        if (!ScriptQuestion("This game is made in GMS2, and you're decompiling to a GMS1 project. The format differences are huge so a LOT of stuff is bound to break.\n\nAre you SURE that you want to continue?"))
        {
            FORM.Close();
            return;
        }
    }

    FORM.Close();

    DUMP = true;

    OBJT = _OBJT.Checked;
    ROOM = _ROOM.Checked;
    SCPT = _SCPT.Checked;
    TMLN = _TMLN.Checked;
    SOND = _SOND.Checked;
    SHDR = _SHDR.Checked;
    PATH = _PATH.Checked;
    FONT = _FONT.Checked;
    SPRT = _SPRT.Checked;
    BGND = _BGND.Checked;
};
FORM.Controls.Add(OKBT);

FORM.Height -= 40;
FORM.AcceptButton = OKBT;
Application.EnableVisualStyles();
FORM.ShowDialog();

// If the export button was never pressed
// exit
if (!DUMP)
    return;

#endregion

#region --------------- Helper Functions ------------
string GetFolder(string path)
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}
string BoolToString(bool value)
{
    // In the GMX file, -1 is true and 0 is false.
    return value ? "-1" : "0";
}

// UnderAnalyzer Decompiling Function
string decompileCode(UndertaleCode codeId)
{
    string code = codeId != null ? new Underanalyzer.Decompiler.DecompileContext(decompileContext, codeId, decompilerSettings).DecompileToString() : "";
    // return code as string to be copied over to .gml file or .gmx asset
    
    return code;
}
#endregion

#region --------------- Start Exporting -------------

if (Directory.Exists(projFolder))
    Directory.Delete(projFolder, true);

Directory.CreateDirectory(projFolder);

var resourceNum = 0;
// Note that I do hate what is below
// but i need it to be in this order

// Find Amount of Assets that will be extracted
if (SPRT) resourceNum += Data.Sprites.Count;
if (BGND) resourceNum += Data.Backgrounds.Count;
if (OBJT) resourceNum += Data.GameObjects.Count;
if (ROOM) resourceNum += Data.Rooms.Count;
if (SOND) resourceNum += Data.Sounds.Count;
if (SCPT) resourceNum += Data.Scripts.Count;
if (FONT) resourceNum += Data.Fonts.Count;
if (PATH) resourceNum += Data.Paths.Count;
if (TMLN) resourceNum += Data.Timelines.Count;
if (SHDR) resourceNum += Data.Shaders.Count;

// Export Resources
if (SPRT) await ExportSprites();
if (BGND) await ExportBackground();
if (OBJT) await ExportGameObjects();
if (ROOM) await ExportRooms();
if (SOND) await ExportSounds();
if (SCPT) await ExportScripts();
if (FONT) await ExportFonts();
if (PATH) await ExportPaths();
if (TMLN) await ExportTimelines();
if (SHDR) await ExportShaders();

// Generate project file
GenerateProjectFile();

#endregion
#region --------------- Export Completed ------------
// changed from .Cleanup due to dumb new utmt shit
worker.Dispose();
HideProgressBar();

if (errLog.Count > 0) // If Errors were Encountered during decompilation
{
    File.WriteAllLinesAsync(projFolder + "Error_Log.txt", errLog);
    ScriptMessage($"Done with {errLog.Count} errors.\n" + projFolder + "\n\nError_Log.txt can be found in the Decompiled Project with a list of all the Scripts and Objects that failed to decompile");
}
else // If there weren't any errors found
    ScriptMessage("Done with No Errors Encountered!\nRemember that this only means that\nScripts and Objects had no problems decompiling");
#endregion

// All Export Functions
#region --------------- Export Sprite ---------------
async Task ExportSprites()
{
    Directory.CreateDirectory(projFolder + "/sprites/images");
    await Task.Run(() => Parallel.ForEach(Data.Sprites, ExportSprite));
}
void ExportSprite(UndertaleSprite sprite)
{
    UpdateProgressBar(null, $"Exporting Sprite: {sprite.Name.Content}", progress++, resourceNum);

    // Save the sprite GMX
    var gmx = new XDocument(
        new XComment(gmxDeclaration),
        new XElement("sprite",
            new XElement("type", "0"),
            new XElement("xorig", sprite.OriginX.ToString()),
            new XElement("yorigin", sprite.OriginY.ToString()),
            new XElement("colkind", sprite.BBoxMode.ToString()),
            new XElement("coltolerance", "0"),
            new XElement("sepmasks", sprite.SepMasks.ToString("D")),
            new XElement("bboxmode", sprite.BBoxMode.ToString()),
            new XElement("bbox_left", sprite.MarginLeft.ToString()),
            new XElement("bbox_right", sprite.MarginRight.ToString()),
            new XElement("bbox_top", sprite.MarginTop.ToString()),
            new XElement("bbox_bottom", sprite.MarginBottom.ToString()),
            new XElement("HTile", "0"),
            new XElement("VTile", "0"),
            new XElement("TextureGroups",
                new XElement("TextureGroup0", "0")
            ),
            new XElement("For3D", "0"),
            new XElement("width", sprite.Width.ToString()),
            new XElement("height", sprite.Height.ToString()),
            new XElement("frames")
        )
    );

    for (int i = 0; i < sprite.Textures.Count; i++)
    {
        if (sprite.Textures[i]?.Texture != null)
        {
            gmx.Element("sprite").Element("frames").Add(
                new XElement(
                    "frame",
                    new XAttribute("index", i.ToString()),
                    "images\\" + sprite.Name.Content + "_" + i + ".png"
                )
            );
        }
    }

    File.WriteAllText(projFolder + "/sprites/" + sprite.Name.Content + ".sprite.gmx", gmx.ToString() + eol);

    // Save sprite images
    for (int i = 0; i < sprite.Textures.Count; i++)
    {
        if (sprite.Textures[i]?.Texture != null)
        {
            worker.ExportAsPNG(sprite.Textures[i].Texture, projFolder + "/sprites/images/" + sprite.Name.Content + "_" + i + ".png", null, true);
        }
    }
}
#endregion
#region --------------- Export Background -----------
async Task ExportBackground()
{
    Directory.CreateDirectory(projFolder + "/background/images");
    await Task.Run(() => Parallel.ForEach(Data.Backgrounds, ExportBackground));
}
void ExportBackground(UndertaleBackground background)
{
    UpdateProgressBar(null, $"Exporting Background: {background.Name.Content}", progress++, resourceNum);

    // Save the backgound GMX
    var gmx = new XDocument(
        new XComment(gmxDeclaration),
        new XElement("background",
            new XElement("istileset", "-1"),
            new XElement("tilewidth", background.Texture == null ? "0" : background.Texture.BoundingWidth.ToString()),
            new XElement("tileheight", background.Texture == null ? "0" : background.Texture.BoundingHeight.ToString()),
            new XElement("tilexoff", "0"),
            new XElement("tileyoff", "0"),
            new XElement("tilehsep", "0"),
            new XElement("tilevsep", "0"),
            new XElement("HTile", "-1"),
            new XElement("VTile", "-1"),
            new XElement("TextureGroups",
                new XElement("TextureGroup0", "0")
            ),
            new XElement("For3D", "0"),
            new XElement("width", background.Texture == null ? "0" : background.Texture.BoundingWidth.ToString()),
            new XElement("height", background.Texture == null ? "0" : background.Texture.BoundingHeight.ToString()),
            new XElement("data", "images\\" + background.Name.Content + ".png")
        )
    );

    File.WriteAllText(projFolder + "/background/" + background.Name.Content + ".background.gmx", gmx.ToString() + eol);

    // Save background images
    if (background.Texture != null)
        worker.ExportAsPNG(background.Texture, projFolder + "/background/images/" + background.Name.Content + ".png");
}
#endregion
#region --------------- Export Object ---------------
async Task ExportGameObjects()
{
    Directory.CreateDirectory(projFolder + "/objects");
    await Task.Run(() => Parallel.ForEach(Data.GameObjects, ExportGameObject));
}
void ExportGameObject(UndertaleGameObject gameObject)
{
    UpdateProgressBar(null, $"Exporting Object: {gameObject.Name.Content}", progress++, resourceNum);

    // Save the object GMX
    var gmx = new XDocument(
        new XComment(gmxDeclaration),
        new XElement("object",
            new XElement("spriteName", gameObject.Sprite is null ? "<undefined>" : gameObject.Sprite.Name.Content),
            new XElement("solid", BoolToString(gameObject.Solid)),
            new XElement("visible", BoolToString(gameObject.Visible)),
            new XElement("depth", gameObject.Depth.ToString()),
            new XElement("persistent", BoolToString(gameObject.Persistent)),
            new XElement("parentName", gameObject.ParentId is null ? "<undefined>" : gameObject.ParentId.Name.Content),
            new XElement("maskName", gameObject.TextureMaskId is null ? "<undefined>" : gameObject.TextureMaskId.Name.Content),
            new XElement("events"),

            //Physics
            new XElement("PhysicsObject", BoolToString(gameObject.UsesPhysics)),
            new XElement("PhysicsObjectSensor", BoolToString(gameObject.IsSensor)),
            new XElement("PhysicsObjectShape", (uint)gameObject.CollisionShape),
            new XElement("PhysicsObjectDensity", gameObject.Density),
            new XElement("PhysicsObjectRestitution", gameObject.Restitution),
            new XElement("PhysicsObjectGroup", gameObject.Group),
            new XElement("PhysicsObjectLinearDamping", gameObject.LinearDamping),
            new XElement("PhysicsObjectAngularDamping", gameObject.AngularDamping),
            new XElement("PhysicsObjectFriction", gameObject.Friction),
            new XElement("PhysicsObjectAwake", BoolToString(gameObject.Awake)),
            new XElement("PhysicsObjectKinematic", BoolToString(gameObject.Kinematic)),
            new XElement("PhysicsShapePoints")
        )
    );



    // Loop through PhysicsShapePoints List
    for (int _point = 0; _point < gameObject.PhysicsVertices.Count; _point++)
    {
        var _x = gameObject.PhysicsVertices[_point].X;
        var _y = gameObject.PhysicsVertices[_point].Y;

        var physicsPointsNode = gmx.Element("object").Element("PhysicsShapePoints");
        physicsPointsNode.Add(new XElement("points", _x.ToString() + "," + _y.ToString()));
    }

    // Traversing the event type list
    for (int i = 0; i < gameObject.Events.Count; i++)
    {
        // Determine if an event is empty
        if (gameObject.Events[i].Count > 0)
        {
            // Traversing event list
            foreach (var j in gameObject.Events[i])
            {
                var eventsNode = gmx.Element("object").Element("events");

                var eventNode = new XElement("event",
                        new XAttribute("eventtype", i.ToString())
                );

                if (j.EventSubtype == 4)
                {
                    // To get the actual name of the collision object when the event type is a collision event
                    eventNode.Add(new XAttribute("ename", Data.GameObjects[(int)j.EventSubtype].Name.Content));
                }
                else
                {
                    // Get the sub-event number directly
                    eventNode.Add(new XAttribute("enumb", j.EventSubtype.ToString()));
                }

                // Save action
                var actionNode = new XElement("action");

                // Traversing the action list
                foreach (var k in j.Actions)
                {
                    actionNode.Add(
                        new XElement("libid", k.LibID.ToString()),
                        new XElement("id", "603"),
                        new XElement("kind", k.Kind.ToString()),
                        new XElement("userelative", BoolToString(k.UseRelative)),
                        new XElement("isquestion", BoolToString(k.IsQuestion)),
                        new XElement("useapplyto", BoolToString(k.UseApplyTo)),
                        new XElement("exetype", k.ExeType.ToString()),
                        new XElement("functionname", k.ActionName.Content),
                        new XElement("codestring", ""),
                        new XElement("whoName", "self"),
                        new XElement("relative", BoolToString(k.Relative)),
                        new XElement("isnot", BoolToString(k.IsNot)),
                        new XElement("arguments",
                            new XElement("argument",
                                new XElement("kind", "1"),
                                new XElement("string", (k.CodeId != null) ? decompileCode(k.CodeId) : "/*DECOMPILER FAILED!*/")
                            )
                        )
                    );
                }
                eventNode.Add(actionNode);
                eventsNode.Add(eventNode);

            }
        }
    }

    File.WriteAllText(projFolder + "/objects/" + gameObject.Name.Content + ".object.gmx", gmx.ToString() + eol);
}
#endregion
#region --------------- Export Room -----------------
async Task ExportRooms()
{
    Directory.CreateDirectory(projFolder + "/rooms");
    await Task.Run(() => Parallel.ForEach(Data.Rooms, ExportRoom));
}
void ExportRoom(UndertaleRoom room)
{
    UpdateProgressBar(null, $"Exporting Room: {room.Name.Content}", progress++, resourceNum);

    // Save the room GMX
    var gmx = new XDocument(
        new XComment(gmxDeclaration),
        new XElement("room",
            new XElement("caption", room.Caption.Content),
            new XElement("width", room.Width.ToString()),
            new XElement("height", room.Height.ToString()),
            new XElement("vsnap", "32"),
            new XElement("hsnap", "32"),
            new XElement("isometric", "0"),
            new XElement("speed", room.Speed.ToString()),
            new XElement("persistent", BoolToString(room.Persistent)),
            // remove alpha (background color doesn't have alpha)
            new XElement("colour", (room.BackgroundColor ^ 0xFF000000).ToString()),
            new XElement("showcolour", BoolToString(room.DrawBackgroundColor)),
            new XElement("code", decompileCode(room.CreationCodeId)),
            new XElement("enableViews", BoolToString(room.Flags.HasFlag(UndertaleRoom.RoomEntryFlags.EnableViews))),
            new XElement("clearViewBackground", BoolToString(room.Flags.HasFlag(UndertaleRoom.RoomEntryFlags.ShowColor))),
            //new XElement("clearDisplayBuffer", BoolToString(room.Flags.HasFlag(UndertaleRoom.RoomEntryFlags.ClearDisplayBuffer))),
            new XElement("makerSettings",
                new XElement("isSet", 0),
                new XElement("w", 1024),
                new XElement("h", 600),
                new XElement("showGrid", 0),
                new XElement("showObjects", -1),
                new XElement("showTiles", -1),
                new XElement("showBackgrounds", -1),
                new XElement("showForegrounds", -1),
                new XElement("showViews", 0),
                new XElement("deleteUnderlyingObj", 0),
                new XElement("deleteUnderlyingTiles", -1),
                new XElement("page", 1),
                new XElement("xoffset", 0),
                new XElement("yoffset", 0)
            )
        )
    );

    // Room backgrounds
    var backgroundsNode = new XElement("backgrounds");
    foreach (var i in room.Backgrounds)
    {
        var backgroundNode = new XElement("background",
            new XAttribute("visible", BoolToString(i.Enabled)),
            new XAttribute("foreground", BoolToString(i.Foreground)),
            new XAttribute("name", i.BackgroundDefinition is null ? "" : i.BackgroundDefinition.Name.Content),
            new XAttribute("x", i.X.ToString()),
            new XAttribute("y", i.Y.ToString()),
            new XAttribute("htiled", BoolToString(i.TiledHorizontally)),
            new XAttribute("vtiled", BoolToString(i.TiledVertically)),
            new XAttribute("hspeed", i.SpeedX.ToString()),
            new XAttribute("vspeed", i.SpeedY.ToString()),
            new XAttribute("stretch", "0")
        );
        backgroundsNode.Add(backgroundNode);
    }
    gmx.Element("room").Add(backgroundsNode);

    // Room views
    var viewsNode = new XElement("views");
    foreach (var i in room.Views)
    {
        var viewNode = new XElement("view",
            new XAttribute("visible", BoolToString(i.Enabled)),
            new XAttribute("objName", i.ObjectId is null ? "<undefined>" : i.ObjectId.Name.Content),
            new XAttribute("xview", i.ViewX.ToString()),
            new XAttribute("yview", i.ViewY.ToString()),
            new XAttribute("wview", i.ViewWidth.ToString()),
            new XAttribute("hview", i.ViewHeight.ToString()),
            new XAttribute("xport", i.PortX.ToString()),
            new XAttribute("yport", i.PortY.ToString()),
            new XAttribute("wport", i.PortWidth.ToString()),
            new XAttribute("hport", i.PortHeight.ToString()),
            new XAttribute("hborder", i.BorderX.ToString()),
            new XAttribute("vborder", i.BorderY.ToString()),
            new XAttribute("hspeed", i.SpeedX.ToString()),
            new XAttribute("vspeed", i.SpeedY.ToString())
        );
        viewsNode.Add(viewNode);
    }
    gmx.Element("room").Add(viewsNode);

    // Room instances
    var instancesNode = new XElement("instances");
    foreach (var i in room.GameObjects)
    {
        var instanceNode = new XElement("instance",
            new XAttribute("objName", i.ObjectDefinition.Name.Content),
            new XAttribute("x", i.X.ToString()),
            new XAttribute("y", i.Y.ToString()),
            new XAttribute("name", "inst_" + i.InstanceID.ToString("X")),
            new XAttribute("locked", "0"),
            new XAttribute("code", decompileCode(i.CreationCode)),
            new XAttribute("scaleX", i.ScaleX.ToString()),
            new XAttribute("scaleY", i.ScaleY.ToString()),
            new XAttribute("colour", i.Color.ToString()),
            new XAttribute("rotation", i.Rotation.ToString())
        );
        instancesNode.Add(instanceNode);
    }
    gmx.Element("room").Add(instancesNode);

    // Room tiles
    var tilesNode = new XElement("tiles");
    foreach (var i in room.Tiles)
    {
        var tileNode = new XElement("tile",
            new XAttribute("bgName", i.BackgroundDefinition is null ? "" : i.BackgroundDefinition.Name.Content),
            new XAttribute("x", i.X.ToString()),
            new XAttribute("y", i.Y.ToString()),
            new XAttribute("w", i.Width.ToString()),
            new XAttribute("h", i.Height.ToString()),
            new XAttribute("xo", i.SourceX.ToString()),
            new XAttribute("yo", i.SourceY.ToString()),
            new XAttribute("id", i.InstanceID.ToString()),
            new XAttribute("name", "inst_" + i.InstanceID.ToString("X")),
            new XAttribute("depth", i.TileDepth.ToString()),
            new XAttribute("locked", "0"),
            new XAttribute("colour", i.Color.ToString()),
            new XAttribute("scaleX", i.ScaleX.ToString()),
            new XAttribute("scaleY", i.ScaleY.ToString())
        );
        tilesNode.Add(tileNode);
    }
    gmx.Element("room").Add(tilesNode);

    //Room Physics

    gmx.Element("room").Add(
        new XElement("PhysicsWorld", BoolToString(room.World)),
        new XElement("PhysicsWorldTop", room.Top),
        new XElement("PhysicsWorldLeft", room.Left),
        new XElement("PhysicsWorldRight", room.Right),
        new XElement("PhysicsWorldBottom", room.Bottom),
        new XElement("PhysicsWorldGravityX", room.GravityX),
        new XElement("PhysicsWorldGravityY", room.GravityY),
        new XElement("PhysicsWorldPixToMeters", room.MetersPerPixel)
    );

    File.WriteAllText(projFolder + "/rooms/" + room.Name.Content + ".room.gmx", gmx.ToString() + eol);
}
#endregion
#region --------------- Export Sound ----------------
async Task ExportSounds()
{
    Directory.CreateDirectory(projFolder + "/sound/audio");
    await Task.Run(() => Parallel.ForEach(Data.Sounds, ExportSound));
}
void ExportSound(UndertaleSound sound)
{
    UpdateProgressBar(null, $"Exporting Sound: {sound.Name.Content}", progress++, resourceNum);

    // Save the sound GMX
    var gmx = new XDocument(
        new XComment(gmxDeclaration),
        new XElement("sound",
            new XElement("kind", Path.GetExtension(sound.File.Content) == ".ogg" ? "3" : "0"),
            new XElement("extension", Path.GetExtension(sound.File.Content)),
            new XElement("origname", "sound\\audio\\" + sound.File.Content),
            new XElement("effects", sound.Effects.ToString()),
            new XElement("volume",
                new XElement("volume", sound.Volume.ToString())
            ),
            new XElement("pan", "0"),
            new XElement("bitRates",
                new XElement("bitRate", "192")
            ),
            new XElement("sampleRates",
                new XElement("sampleRate", "44100")
            ),
            new XElement("types",
                new XElement("type", "1")
            ),
            new XElement("bitDepths",
                new XElement("bitDepth", "16")
            ),
            new XElement("preload", "-1"),
            new XElement("data", Path.GetFileName(sound.File.Content)),
            new XElement("compressed", Path.GetExtension(sound.File.Content) == ".ogg" ? "1" : "0"),
            new XElement("streamed", Path.GetExtension(sound.File.Content) == ".ogg" ? "1" : "0"),
            new XElement("uncompressOnLoad", "0"),
            new XElement("audioGroup", "0")
        )
    );

    File.WriteAllText(projFolder + "/sound/" + sound.Name.Content + ".sound.gmx", gmx.ToString() + eol);

    // Save sound files
    if (sound.AudioFile != null)
        File.WriteAllBytes(projFolder + "/sound/audio/" + sound.File.Content, sound.AudioFile.Data);
    // if sound file is external, add them
    else if (File.Exists($"{Path.GetDirectoryName(FilePath)}\\" + sound.File.Content))
        File.Copy($"{Path.GetDirectoryName(FilePath)}\\" + sound.File.Content, projFolder + "/sound/audio/" + sound.File.Content, true);
}
#endregion
#region --------------- Export Script ---------------
async Task ExportScripts()
{
    Directory.CreateDirectory(projFolder + "/scripts/");
    await Task.Run(() => Parallel.ForEach(Data.Scripts, ExportScript));
}
void ExportScript(UndertaleScript script)
{
    UpdateProgressBar(null, $"Exporting Script: {script.Name.Content}", progress++, resourceNum);

    // Save GML files, and detect decompilation failure
    try
    {
        File.WriteAllText(projFolder + "/scripts/" + script.Name.Content + ".gml", decompileCode(script.Code));
    }
    catch (Exception e)
    {
        File.WriteAllText(projFolder + "/scripts/" + script.Name.Content + ".gml", "/*\nDECOMPILER FAILED!\n\n" + e.ToString() + "\n*/");
        // To list what Script failed to decompile
        errLog.Add($"SCRIPT:   {script.Name.Content}");
    }
}
#endregion
#region --------------- Export Font -----------------
async Task ExportFonts()
{
    Directory.CreateDirectory(projFolder + "/fonts/");
    await Task.Run(() => Parallel.ForEach(Data.Fonts, ExportFont));
}
void ExportFont(UndertaleFont font)
{
    UpdateProgressBar(null, $"Exporting Font: {font.Name.Content}", progress++, resourceNum);

    // Save the font GMX
    var gmx = new XDocument(
        new XComment(gmxDeclaration),
        new XElement("font",
            new XElement("name", font.Name.Content),
            new XElement("size", font.EmSize.ToString()),
            new XElement("bold", BoolToString(font.Bold)),
            new XElement("renderhq", "-1"),
            new XElement("italic", BoolToString(font.Italic)),
            new XElement("charset", font.Charset.ToString()),
            new XElement("aa", font.AntiAliasing.ToString()),
            new XElement("includeTTF", "0"),
            new XElement("TTFName", ""),
            new XElement("texgroups",
                new XElement("texgroup", "0")
            ),
            new XElement("ranges",
                new XElement("range0", font.RangeStart.ToString() + "," + font.RangeEnd.ToString())
            ),
            new XElement("glyphs"),
            new XElement("kerningPairs"),
            new XElement("image", font.Name.Content + ".png")
        )
    );

    var glyphsNode = gmx.Element("font").Element("glyphs");
    foreach (var i in font.Glyphs)
    {
        var glyphNode = new XElement("glyph");
        glyphNode.Add(new XAttribute("character", i.Character.ToString()));
        glyphNode.Add(new XAttribute("x", i.SourceX.ToString()));
        glyphNode.Add(new XAttribute("y", i.SourceY.ToString()));
        glyphNode.Add(new XAttribute("w", i.SourceWidth.ToString()));
        glyphNode.Add(new XAttribute("h", i.SourceHeight.ToString()));
        glyphNode.Add(new XAttribute("shift", i.Shift.ToString()));
        glyphNode.Add(new XAttribute("offset", i.Offset.ToString()));
        glyphsNode.Add(glyphNode);
    }

    File.WriteAllText(projFolder + "/fonts/" + font.Name.Content + ".font.gmx", gmx.ToString() + eol);

    // Save font textures
    worker.ExportAsPNG(font.Texture, projFolder + "/fonts/" + font.Name.Content + ".png");
}
#endregion
#region --------------- Export Paths ----------------
async Task ExportPaths()
{
    Directory.CreateDirectory(projFolder + "/paths");
    await Task.Run(() => Parallel.ForEach(Data.Paths, ExportPath));
}
void ExportPath(UndertalePath path)
{
    UpdateProgressBar(null, $"Exporting Path: {path.Name.Content}", progress++, resourceNum);

    // Save the path GMX
    var gmx = new XDocument(
        new XComment(gmxDeclaration),
        new XElement("path",
            new XElement("kind", "0"),
            new XElement("closed", BoolToString(path.IsClosed)),
            new XElement("precision", path.Precision.ToString()),
            new XElement("backroom", "-1"),
            new XElement("hsnap", "16"),
            new XElement("vsnap", "16"),
            new XElement("points")
        )
    );
    foreach (var i in path.Points)
    {
        var pointsNode = gmx.Element("path").Element("points");
        pointsNode.Add(
            new XElement("point", $"{i.X.ToString()},{i.Y.ToString()},{i.Speed.ToString()}")
        );
    }

    File.WriteAllText(projFolder + "/paths/" + path.Name.Content + ".path.gmx", gmx.ToString() + eol);
}
#endregion
#region --------------- Export Timelines ------------
async Task ExportTimelines()
{
    Directory.CreateDirectory(projFolder + "/timelines");
    await Task.Run(() => Parallel.ForEach(Data.Timelines, ExportTimeline));
}
void ExportTimeline(UndertaleTimeline timeline)
{
    UpdateProgressBar(null, $"Exporting Timeline: {timeline.Name.Content}", progress++, resourceNum);

    // Save the timeline GMX
    var gmx = new XDocument(
        new XComment(gmxDeclaration),
        new XElement("timeline")
    );
    foreach (var i in timeline.Moments)
    {
        var entryNode = new XElement("entry");
        entryNode.Add(new XElement("step", i.Step));
        entryNode.Add(new XElement("event"));
        foreach (var j in i.Event)
        {
            entryNode.Element("event").Add(
                new XElement("action",
                    new XElement("libid", j.LibID.ToString()),
                    new XElement("id", "603"),
                    new XElement("kind", j.Kind.ToString()),
                    new XElement("userelative", BoolToString(j.UseRelative)),
                    new XElement("isquestion", BoolToString(j.IsQuestion)),
                    new XElement("useapplyto", BoolToString(j.UseApplyTo)),
                    new XElement("exetype", j.ExeType.ToString()),
                    new XElement("functionname", j.ActionName.Content),
                    new XElement("codestring", ""),
                    new XElement("whoName", "self"),
                    new XElement("relative", BoolToString(j.Relative)),
                    new XElement("isnot", BoolToString(j.IsNot)),
                    new XElement("arguments",
                        new XElement("argument",
                            new XElement("kind", "1"),
                            new XElement("string", decompileCode(j.CodeId))
                        )
                    )
                )
            );
        }
        gmx.Element("timeline").Add(entryNode);
    }

    File.WriteAllText(projFolder + "/timelines/" + timeline.Name.Content + ".timeline.gmx", gmx.ToString() + eol);
}
#endregion
#region --------------- Export Shaders --------------
async Task ExportShaders()
{
    Directory.CreateDirectory(projFolder + "/shaders");
    await Task.Run(() => Parallel.ForEach(Data.Shaders, ExportShader));
}
void ExportShader(UndertaleShader shader)
{
    // Vertex and Fragment shit
    var vertex = shader.GLSL_ES_Vertex.Content;
    var fragment = shader.GLSL_ES_Fragment.Content;

    // to avoid declaring useless shit
    if (vertex != null && fragment != null)
    {
        string splitter = "#define _YY_GLSLES_ 1\n";
        if (vertex.Contains(splitter))
            vertex = vertex.Substring(vertex.IndexOf(splitter) + splitter.Length);
        if (fragment.Contains(splitter))
            fragment = fragment.Substring(fragment.IndexOf(splitter) + splitter.Length);
    }

    UpdateProgressBar(null, $"Exporting Script: {shader.Name.Content}", progress++, resourceNum);
    File.WriteAllText(projFolder + "/shaders/" + shader.Name.Content + ".shader", vertex + "\n//######################_==_YOYO_SHADER_MARKER_==_######################@~//\n" + fragment);
}
#endregion

#region --------------- Generate Project File -------
void GenerateProjectFile()
{
    UpdateProgressBar(null, $"Generating Project File...", progress++, resourceNum);

    var gmx = new XDocument(
        new XComment(gmxDeclaration),
        new XElement("assets")
    );

    // Write all resource indexes to project.gmx
    if (SOND) WriteIndexes<UndertaleSound>(gmx.Element("assets"), "sounds", "sound", Data.Sounds, "sound", "sound\\");
    if (SPRT) WriteIndexes<UndertaleSprite>(gmx.Element("assets"), "sprites", "sprites", Data.Sprites, "sprite", "sprites\\");
    if (BGND) WriteIndexes<UndertaleBackground>(gmx.Element("assets"), "backgrounds", "background", Data.Backgrounds, "background", "background\\");
    if (SCPT) WriteIndexes<UndertaleScript>(gmx.Element("assets"), "scripts", "scripts", Data.Scripts, "script", "scripts\\", ".gml");
    if (FONT) WriteIndexes<UndertaleFont>(gmx.Element("assets"), "fonts", "fonts", Data.Fonts, "font", "fonts\\");
    if (OBJT) WriteIndexes<UndertaleGameObject>(gmx.Element("assets"), "objects", "objects", Data.GameObjects, "object", "objects\\");
    if (ROOM) WriteIndexes<UndertaleRoom>(gmx.Element("assets"), "rooms", "rooms", Data.Rooms, "room", "rooms\\");
    if (PATH) WriteIndexes<UndertalePath>(gmx.Element("assets"), "paths", "paths", Data.Paths, "path", "paths\\");
    if (TMLN) WriteIndexes<UndertaleTimeline>(gmx.Element("assets"), "timelines", "timelines", Data.Timelines, "timeline", "timelines\\");
    if (SHDR) WriteIndexes<UndertaleShader>(gmx.Element("assets"), "shaders", "shaders", Data.Shaders, "shader", "shaders\\", ".shader");

    File.WriteAllText(projFolder + GameName + ".project.gmx", gmx.ToString() + eol);
}

void WriteIndexes<T>(XElement rootNode, string elementName, string attributeName, IList<T> dataList, string oneName, string resourcePath, string fileExtension = "")
{
    var resourcesNode = new XElement(elementName,
        new XAttribute("name", attributeName)
    );
    foreach (UndertaleNamedResource i in dataList)
    {
        var resourceNode = new XElement(oneName, resourcePath + i.Name.Content + fileExtension);
        resourcesNode.Add(resourceNode);
    }
    rootNode.Add(resourcesNode);
}
#endregion
