using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GTA;

namespace DeveloperConsole {
    /// <summary>
    /// The object selector
    /// </summary>
    public class ObjectSelector {
        private readonly float _maxDist = 10000;
        private int _lastHandle;
        private Entity _selectedEntity;

        /// <summary>
        /// Creates an ObjectSelector
        /// </summary>
        public ObjectSelector() {
            Enabled = false;
        }

        /// <summary>
        /// A dictionary of entities where the key is the entities handle and the value is the entity
        /// </summary>
        public SortedDictionary<int, Entity> Entities { get; private set; }
        /// <summary>
        /// Where or not the object selector is enabled
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// This method should be called each tick
        /// </summary>
        public void Tick() {
            if (_selectedEntity != null && !_selectedEntity.Exists()) _selectedEntity = null;

            Draw();
        }

        /// <summary>
        /// Calls the object selector
        /// </summary>
        public void Draw() {
            if (Enabled) {
                GTAFuncs.ShowCursorThisFrame();
                GTAFuncs.DisplayHud(false);
                GTAFuncs.DisplayRadar(false);
                DrawEnts();
            }
            else {
                GTAFuncs.DisplayHud(true);
                GTAFuncs.DisplayRadar(true);
            }
        }

        /// <summary>
        /// Handles keypresses, should be called from the scripts KeyDown event
        /// </summary>
        /// <param name="sender">The object sending the event</param>
        /// <param name="e">The event arguments</param>
        public void KeyPress(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Tab) {
                if ((e.Modifiers & Keys.Control) == Keys.Control) {
                    if (Enabled) {
                        Enabled = false;
                        if (_selectedEntity == null) return;
                        var returnText = "return ";
                        if (DeveloperConsole.Instance.Input.StartsWith("cs")) returnText = "";
                        if (_selectedEntity is Vehicle) {
                            DeveloperConsole.Instance.Input += " {" + returnText + "new Vehicle(" +
                                                               _selectedEntity.Handle + ")} ";
                        }
                        else if (_selectedEntity is Ped) {
                            DeveloperConsole.Instance.Input += " {" + returnText + "new Ped(" + _selectedEntity.Handle +
                                                               ")} ";
                        }
                        else {
                            DeveloperConsole.Instance.Input += " {" + returnText + "new Entity(" +
                                                               _selectedEntity.Handle + ")} ";
                        }
                        DeveloperConsole.Instance.ShowConsole(true);
                    }
                    else {
                        Enabled = true;
                    }
                }
                else {
                    if (!Enabled) return;
                    _selectedEntity = GetNextEntity();
                    _lastHandle = _selectedEntity.Handle;
                }
            }
        }

        /// <summary>
        /// Gets the next entity in the object selector
        /// </summary>
        /// <returns>The next entity</returns>
        private Entity GetNextEntity() {
            if (Entities.Count < 1) return null;
            var last = Entities.Last().Key;
            if (_lastHandle > last) _lastHandle = 0;
            while (_lastHandle++ < last) {
                if (Entities.ContainsKey(_lastHandle)) return Entities[_lastHandle];
            }
            _lastHandle = 0;
            return GetNextEntity();
        }

        /// <summary>
        /// Draw all the entities we can see
        /// </summary>
        private void DrawEnts() {
            var textScale = .25f;

            var normal = Color.FromArgb(255, Color.Yellow.R, Color.Yellow.G, Color.Yellow.B);
            var selected = Color.FromArgb(255, Color.Red.R, Color.Red.G, Color.Red.B);

            Entities = new SortedDictionary<int, Entity>();

            foreach (var p in World.GetNearbyPeds(Game.Player.Character, _maxDist)) {
                if (p.IsOnScreen && !p.IsOccluded) {
                    Entities.Add(p.Handle, p);

                    var boxColor = Color.FromArgb(150, Color.Yellow);
                    var c = normal;
                    var prefix = "";
                    if (_selectedEntity != null && p.Equals(_selectedEntity)) {
                        c = selected;
                        prefix = "** ";
                        boxColor = Color.Red;
                    }
                    var pos = GTAFuncs.WorldToScreen(p.Position);
                    GTAFuncs.SetTextDropShadow(2, Color.FromArgb(255, 0, 0, 0));
                    new UIText(prefix + "Ped #" + p.Handle,
                        new Point((int) pos.X, (int) pos.Y + (p.IsInVehicle() ? -10 : 0)), textScale, c, 0, true).Draw();
                    DrawEntBox(p, boxColor);
                }
            }

            foreach (var v in World.GetNearbyVehicles(Game.Player.Character, _maxDist)) {
                if (v.IsOnScreen && !v.IsOccluded) {
                    Entities.Add(v.Handle, v);

                    var boxColor = Color.FromArgb(150, Color.DeepPink);
                    var c = normal;
                    var prefix = "";
                    if (_selectedEntity != null && v.Equals(_selectedEntity)) {
                        c = selected;
                        prefix = "** ";
                        boxColor = Color.Red;
                    }

                    var pos = GTAFuncs.WorldToScreen(v.Position);
                    GTAFuncs.SetTextDropShadow(2, Color.FromArgb(255, 0, 0, 0));
                    new UIText(prefix + "Vehicle #" + v.Handle, new Point((int) pos.X, (int) pos.Y), textScale, c, 0,
                        true).Draw();
                    GTAFuncs.SetTextDropShadow(2, Color.FromArgb(255, 0, 0, 0));
                    new UIText(v.FriendlyName, new Point((int) pos.X, (int) pos.Y + 10), textScale, c, 0, true).Draw();
                    GTAFuncs.SetTextDropShadow(2, Color.FromArgb(255, 0, 0, 0));
                    new UIText(v.DisplayName + v.Handle, new Point((int) pos.X, (int) pos.Y + 20), textScale, c, 0, true)
                        .Draw();
                    DrawEntBox(v, boxColor);
                }
            }

            GTAFuncs.SetTextDropShadow(0, Color.Transparent);
        }

        /// <summary>
        /// Draws a box around the specified entity
        /// </summary>
        /// <param name="e">The entity to draw around</param>
        /// <param name="c">The box color</param>
        public void DrawEntBox(Entity e, Color c) {
            var size = e.Model.GetDimensions();
            var location = e.Position - (size/2);
            var r = new Rectangle3D(location, size).Rotate(GTAFuncs.GetEntityQuaternion(e)).DrawWireFrame(c, true);
        }
    }
}