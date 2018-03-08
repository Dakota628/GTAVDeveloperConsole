using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Media.Media3D;
using GTA;
using GTA.Math;
using Quaternion = GTA.Math.Quaternion;

namespace DeveloperConsole {
    /// <summary>
    ///     Represents a 3D line
    /// </summary>
    public class Line3D {
        /// <summary>
        ///     Create a Line3D
        /// </summary>
        /// <param name="p1">The first point in the line</param>
        /// <param name="p2">The second point in the line</param>
        public Line3D(Vector3 p1, Vector3 p2) {
            Point1 = p1;
            Point2 = p2;
        }

        /// <summary>
        ///     The first point in the line
        /// </summary>
        public Vector3 Point1 { get; private set; }

        /// <summary>
        ///     The second point in the line
        /// </summary>
        public Vector3 Point2 { get; private set; }

        /// <summary>
        /// 	Draw the line
        /// </summary>
        public void Draw(Color c) {
            GTAFuncs.DrawLine(Point1, Point2, c);
        }
    }

    /// <summary>
    ///     Represents a 3D face
    /// </summary>
    public class Face3D {
        /// <summary>
        ///     Create a Face3D
        /// </summary>
        /// <param name="bottomLeft">The bottom left corner of the face</param>
        /// <param name="topRight">The top right corner of the face</param>
        /// <param name="topLeft">The top left corner of the face</param>
        /// <param name="bottomRight">The bottom right corner of the face</param>
        public Face3D(Vector3 bottomLeft, Vector3 topRight, Vector3 topLeft, Vector3 bottomRight) {
            BottomLeft = bottomLeft;
            TopRight = topRight;
            TopLeft = topLeft;
            BottomRight = bottomRight;
        }

        /// <summary>
        ///     The bottom left corner of the face
        /// </summary>
        public Vector3 BottomLeft { get; private set; }

        /// <summary>
        ///     The top right corner of the face
        /// </summary>
        public Vector3 TopRight { get; private set; }

        /// <summary>
        ///     The top left corner of the face
        /// </summary>
        public Vector3 TopLeft { get; private set; }

        /// <summary>
        ///     The bottom right corner of the face
        /// </summary>
        public Vector3 BottomRight { get; private set; }

        /// <summary>
        ///     Draws the face
        /// </summary>
        /// <param name="c">The color of the face</param>
        public void Draw(Color c) {
            GTAFuncs.DrawBox(BottomLeft, TopRight, c);
        }
    }

    /// <summary>
    ///     Represents a 3D Rectangle
    /// </summary>
    public class Rectangle3D {
        /// <summary>
        ///     Create a Rectangle3D
        /// </summary>
        /// <param name="center">The center of the rectangle.</param>
		/// <param name="minimum">The minimum dimensions.</param>
		/// <param name="maximum">The maximum dimensions.</param>
        public Rectangle3D(Vector3 center, Vector3 min, Vector3 max) {
            Center = center;
            Min = min;
            Max = max;
                
            Corners = new Dictionary<string, Vector3> {
                {"000", center + new Vector3(Min.X,Min.Y,Min.Z)},
                {"100", center + new Vector3(Max.X,Min.Y,Min.Z)},
                {"010", center + new Vector3(Min.X,Max.Y,Min.Z)},
                {"001", center + new Vector3(Max.X,Max.Y,Min.Z)},
                {"110", center + new Vector3(Min.X,Min.Y,Max.Z)},
                {"101", center + new Vector3(Max.X,Min.Y,Max.Z)},
                {"011", center + new Vector3(Min.X,Max.Y,Max.Z)},
                {"111", center + new Vector3(Max.X,Max.Y,Max.Z)}
            };

            GenerateEdges();
            GenerateFaces();
        }

        /// <summary>
        ///     The center position of the rectangle
        /// </summary>
        public Vector3 Center { get; private set; }

        /// <summary>
        ///     The minimum dimensions.
        /// </summary>
        public Vector3 Min { get; private set; }

        /// <summary>
        ///     The maximum dimensions.
        /// </summary>
        public Vector3 Max { get; private set; }

        /// <summary>
        ///     The corners of the rectangle where the string is the corner vector and the value is the location of the corner
        /// </summary>
        public Dictionary<string, Vector3> Corners { get; private set; }

        /// <summary>
        ///     The edges of the rectangle
        /// </summary>
        public List<Line3D> Edges { get; private set; }

        /// <summary>
        ///     The faces of the rectangle
        /// </summary>
        public List<Face3D> Faces { get; private set; }

        /// <summary>
        ///     Generate the edges of the rectangle, should be called when modifying corners
        /// </summary>
        private void GenerateEdges() {
            Edges = new List<Line3D> {
                new Line3D(Corners["000"], Corners["001"]),
                new Line3D(Corners["000"], Corners["100"]),
                new Line3D(Corners["100"], Corners["101"]),
                new Line3D(Corners["001"], Corners["101"]),
                new Line3D(Corners["001"], Corners["011"]),
                new Line3D(Corners["000"], Corners["010"]),
                new Line3D(Corners["100"], Corners["110"]),
                new Line3D(Corners["101"], Corners["111"]),
                new Line3D(Corners["110"], Corners["111"]),
                new Line3D(Corners["011"], Corners["111"]),
                new Line3D(Corners["010"], Corners["110"]),
                new Line3D(Corners["010"], Corners["011"])
            };
        }

        /// <summary>
        ///     Generate the faces of the rectangl, should be called when modifying corners
        /// </summary>
        private void GenerateFaces() {
            Faces = new List<Face3D> {
                new Face3D(Corners["000"], Corners["101"], Corners["001"], Corners["100"]),
                new Face3D(Corners["100"], Corners["111"], Corners["101"], Corners["110"]),
                new Face3D(Corners["110"], Corners["011"], Corners["111"], Corners["010"]),
                new Face3D(Corners["010"], Corners["001"], Corners["011"], Corners["000"]),
                new Face3D(Corners["101"], Corners["011"], Corners["001"], Corners["111"]),
                new Face3D(Corners["100"], Corners["010"], Corners["000"], Corners["110"])
            };
        }

        /// <summary>
        ///     Rotate the rectangle by a quaternion
        /// </summary>
        /// <param name="rot">The quaternion to rotate by</param>
        /// <returns>The current rectangle instance</returns>
        public Rectangle3D Rotate(Quaternion rot) {
            var q = new QuaternionRotation3D(new System.Windows.Media.Media3D.Quaternion(rot.X, rot.Y, rot.Z, rot.W));
            var r = new RotateTransform3D(q, ToPoint3D(Center));
            foreach (var k in new List<string>(Corners.Keys))
                Corners[k] = ToVector3(r.Transform(ToPoint3D(Corners[k])));
            GenerateEdges();
            GenerateFaces();
            return this;
        }

        /// <summary>
        ///     Draw the rectangles wireframe without diagonals
        /// </summary>
        /// <param name="c">The color of the wire frame</param>
        /// <returns>The current rectangle instance</returns>
        public Rectangle3D DrawWireFrame(Color c) {
            return DrawWireFrame(c, false);
        }

        /// <summary>
        ///     Draw the rectangles wireframe
        /// </summary>
        /// <param name="c">The color of the wireframe</param>
        /// <param name="diagonals">Whether or not to draw diagonals</param>
        /// <returns>The current rectangle instance</returns>
        public Rectangle3D DrawWireFrame(Color c, bool diagonals) {
            foreach (var e in Edges) e.Draw(c);
            if (diagonals) {
                foreach (var f in Faces) {
                    new Line3D(f.BottomLeft, f.TopRight).Draw(c);
                    new Line3D(f.BottomRight, f.TopLeft).Draw(c);
                }
            }
            return this;
        }

        /// <summary>
        ///     Draw all the rectangle
        /// </summary>
        /// <param name="c">The color of the rectangle</param>
        /// <returns>The current rectangle instance</returns>
        public Rectangle3D Draw(Color c) {
            foreach (var f in Faces) f.Draw(c);
            return this;
        }

        /// <summary>
        ///     Draw a 2D overlay of the 3D rectangle debug
        /// </summary>
        /// <param name="c">The color of the debug overlay</param>
        /// <returns>The current rectangle instance</returns>
        public Rectangle3D DrawDebug(Color c) {
            foreach (var v in Corners) {
                var w = GTAFuncs.WorldToScreen(v.Value);
                new UIText(v.Key, new Point((int) w.X, (int) w.Y), .15f, c).Draw();
            }
            return this;
        }

        /// <summary>
        ///     Converts a Point3D to a Vector3
        /// </summary>
        /// <param name="p">The Point3D</param>
        /// <returns>The Vector3</returns>
        private Vector3 ToVector3(Point3D p) {
            return new Vector3(Convert.ToSingle(p.X), Convert.ToSingle(p.Y), Convert.ToSingle(p.Z));
        }

        /// <summary>
        ///     Converts a Vector3 to a Point3D
        /// </summary>
        /// <param name="v">The Vector3</param>
        /// <returns>The Point3D</returns>
        private Point3D ToPoint3D(Vector3 v) {
            return new Point3D(v.X, v.Y, v.Z);
        }
    }
}
