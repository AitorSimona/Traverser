using UnityEngine;
using Unity.Mathematics;
using System.Collections.Generic;
using UnityEngine.Rendering;
using System;

namespace Unity.Kinematica
{
    public static partial class DebugDraw
    {
        internal enum RenderingPipeline
        {
            Undefined,
            Legacy,
            HDRP,
        }

        internal static Color White = Color.white;
        internal static Color Black = Color.black;
        internal static Color Red = Color.red;
        internal static Color DarkRed = new Color(0.75f, 0f, 0f, 1f);
        internal static Color Green = Color.green;
        internal static Color DarkGreen = new Color(0f, 0.75f, 0f, 1f);
        internal static Color Blue = Color.blue;
        internal static Color Cyan = Color.cyan;
        internal static Color Magenta = Color.magenta;
        internal static Color Yellow = Color.yellow;
        internal static Color Grey = Color.grey;
        internal static Color LightGrey = new Color(0.75f, 0.75f, 0.75f, 1f);
        internal static Color DarkGrey = new Color(0.25f, 0.25f, 0.25f, 1f);
        internal static Color Orange = new Color(1f, 0.5f, 0f, 1f);
        internal static Color Brown = new Color(0.5f, 0.25f, 0f, 1f);
        internal static Color Mustard = new Color(1f, 0.75f, 0.25f, 1f);
        internal static Color Teal = new Color(0f, 0.75f, 0.75f, 1f);
        internal static Color Purple = new Color(0.5f, 0f, 0.5f, 1f);

        static RenderingPipeline s_CurrentRenderingPipeline = RenderingPipeline.Undefined;

        internal static RenderingPipeline CurrentRenderingPipeline
        {
            get
            {
                if (s_CurrentRenderingPipeline == RenderingPipeline.Undefined)
                {
                    if (GraphicsSettings.renderPipelineAsset != null && GraphicsSettings.renderPipelineAsset.GetType().Name.Contains("HDRenderPipelineAsset"))
                    {
                        s_CurrentRenderingPipeline = RenderingPipeline.HDRP;
                    }
                    else
                    {
                        s_CurrentRenderingPipeline = RenderingPipeline.Legacy;
                    }
                }

                return s_CurrentRenderingPipeline;
            }
        }

        internal static bool IsHDRP => CurrentRenderingPipeline == RenderingPipeline.HDRP;

        public static void Begin()
        {
            if (Active)
            {
                Debug.Log("Drawing is still active. Call 'End()' to stop.");
            }
            else
            {
                Initialize();
                Camera = GetCamera();
                ViewPosition = Camera.transform.position;
                ViewRotation = Camera.transform.rotation;
                Active = true;
            }
        }

        public static void Begin(Camera camera)
        {
            if (Active)
            {
                Debug.Log("Drawing is still active. Call 'End()' to stop.");
            }
            else
            {
                Initialize();
                Camera = camera;
                ViewPosition = Camera.transform.position;
                ViewRotation = Camera.transform.rotation;
                Active = true;
            }
        }

        public static void End()
        {
            if (Active)
            {
                SetProgram(PROGRAM.NONE);
                Camera = null;
                ViewPosition = Vector3.zero;
                ViewRotation = Quaternion.identity;
                Active = false;
            }
            else
            {
                Debug.Log("Drawing is not active. Call 'Begin()' to start.");
            }
        }

        internal static void SetDepthRendering(bool enabled)
        {
            Initialize();
            SetProgram(PROGRAM.NONE);
            GLMaterial.SetInt("_ZWrite", enabled ? 1 : 0);
            GLMaterial.SetInt("_ZTest",
                enabled
                ? (int)UnityEngine.Rendering.CompareFunction.LessEqual
                : (int)UnityEngine.Rendering.CompareFunction.Always);
            MeshMaterial.SetInt("_ZWrite", enabled ? 1 : 0);
            MeshMaterial.SetInt("_ZTest",
                enabled
                ? (int)UnityEngine.Rendering.CompareFunction.LessEqual
                : (int)UnityEngine.Rendering.CompareFunction.Always);
        }

        internal static void SetCurvature(float value)
        {
            Initialize();
            SetProgram(PROGRAM.NONE);
            MeshMaterial.SetFloat("_Power", value);
        }

        internal static void SetFilling(float value)
        {
            value = Mathf.Clamp(value, 0f, 1f);
            Initialize();
            SetProgram(PROGRAM.NONE);
            MeshMaterial.SetFloat("_Filling", value);
        }

        internal static void DrawLine(Vector3 start, Vector3 end, Color color)
        {
            if (Return())
            {
                return;
            }

            if (IsHDRP)
            {
                Debug.DrawLine(start, end, color);
            }
            else
            {
                SetProgram(PROGRAM.LINES);
                GL.Color(color);
                GL.Vertex(start);
                GL.Vertex(end);
            }
        }

        internal static void DrawTransform(AffineTransform transform, float scale, float duration = 0.0f)
        {
            Debug.DrawLine(transform.t, transform.t + Missing.xaxis(transform.q) * scale, Color.red, duration);
            Debug.DrawLine(transform.t, transform.t + Missing.yaxis(transform.q) * scale, Color.green, duration);
            Debug.DrawLine(transform.t, transform.t + Missing.zaxis(transform.q) * scale, Color.blue, duration);
        }

        internal static void DrawLine(Vector3 start, Vector3 end, float thickness, Color color)
        {
            DrawLine(start, end, thickness, thickness, color);
        }

        internal static void DrawLine(Vector3 start, Vector3 end, float startThickness, float endThickness, Color color)
        {
            if (Return())
            {
                return;
            }

            SetProgram(PROGRAM.QUADS);
            GL.Color(color);
            Vector3 dir = (end - start).normalized;
            Vector3 orthoStart = startThickness / 2f * (Quaternion.AngleAxis(90f, (start - ViewPosition)) * dir);
            Vector3 orthoEnd = endThickness / 2f * (Quaternion.AngleAxis(90f, (end - ViewPosition)) * dir);
            GL.Vertex(end + orthoEnd);
            GL.Vertex(end - orthoEnd);
            GL.Vertex(start - orthoStart);
            GL.Vertex(start + orthoStart);
        }

        internal static void DrawTriangle(Vector3 a, Vector3 b, Vector3 c, Color color)
        {
            if (Return())
            {
                return;
            }

            if (IsHDRP)
            {
                DrawLine(b, a, color);
                DrawLine(a, c, color);
                DrawLine(c, b, color);
            }
            else
            {
                SetProgram(PROGRAM.TRIANGLES);
                GL.Color(color);
                GL.Vertex(b);
                GL.Vertex(a);
                GL.Vertex(c);
            }
        }

        internal static void DrawArrow(AffineTransform transform, Color arrowColor, Color lineColor, float scale)
        {
            float width = 0.2f * scale;
            float length = 0.3f * scale;
            float tipWidth = 0.5f * scale;
            float tipheight = 0.8f * scale;

            Vector3 p0 = transform.transform(new Vector3(-width, 0.0f, -length));
            Vector3 p1 = transform.transform(new Vector3(-width, 0.0f, length));
            Vector3 p2 = transform.transform(new Vector3(width, 0.0f, length));
            Vector3 p3 = transform.transform(new Vector3(width, 0.0f, -length));

            DrawTriangle(p2, p1, p0, arrowColor);
            DrawTriangle(p0, p3, p2, arrowColor);

            Vector3 p4 = transform.transform(new Vector3(-tipWidth, 0.0f, length));
            Vector3 p5 = transform.transform(new Vector3(0.0f, 0.0f, tipheight));
            Vector3 p6 = transform.transform(new Vector3(tipWidth, 0.0f, length));

            DrawTriangle(p6, p5, p4, arrowColor);

            DrawLine(p1, p0, lineColor);
            DrawLine(p0, p3, lineColor);
            DrawLine(p3, p2, lineColor);
            DrawLine(p2, p6, lineColor);
            DrawLine(p6, p5, lineColor);
            DrawLine(p5, p4, lineColor);
            DrawLine(p4, p1, lineColor);
        }

        internal static void DrawCircle(Vector3 position, float size, Color color)
        {
            DrawMesh(CircleMesh, position, ViewRotation, size * Vector3.one, color);
        }

        internal static void DrawCircle(Vector3 position, Quaternion rotation, float size, Color color)
        {
            DrawMesh(CircleMesh, position, rotation, size * Vector3.one, color);
        }

        internal static void DrawWireCircle(Vector3 position, float size, Color color)
        {
            DrawWire(CircleWire, position, ViewRotation, size * Vector3.one, color);
        }

        internal static void DrawWireCircle(Vector3 position, Quaternion rotation, float size, Color color)
        {
            DrawWire(CircleWire, position, rotation, size * Vector3.one, color);
        }

        internal static void DrawWiredCircle(Vector3 position, float size, Color circleColor, Color wireColor)
        {
            DrawCircle(position, size, circleColor);
            DrawWireCircle(position, size, wireColor);
        }

        internal static void DrawWiredCircle(Vector3 position, Quaternion rotation, float size, Color circleColor,
            Color wireColor)
        {
            DrawCircle(position, rotation, size, circleColor);
            DrawWireCircle(position, rotation, size, wireColor);
        }

        internal static void DrawEllipse(Vector3 position, float width, float height, Color color)
        {
            DrawMesh(CircleMesh, position, ViewRotation, new Vector3(width, height, 1f), color);
        }

        internal static void DrawEllipse(Vector3 position, Quaternion rotation, float width, float height, Color color)
        {
            DrawMesh(CircleMesh, position, rotation, new Vector3(width, height, 1f), color);
        }

        internal static void DrawWireEllipse(Vector3 position, float width, float height, Color color)
        {
            DrawWire(CircleWire, position, ViewRotation, new Vector3(width, height, 1f), color);
        }

        internal static void DrawWireEllipse(Vector3 position, Quaternion rotation, float width, float height, Color color)
        {
            DrawWire(CircleWire, position, rotation, new Vector3(width, height, 1f), color);
        }

        internal static void DrawWiredEllipse(Vector3 position, float width, float height, Color ellipseColor, Color wireColor)
        {
            DrawEllipse(position, ViewRotation, width, height, ellipseColor);
            DrawWireEllipse(position, ViewRotation, width, height, wireColor);
        }

        internal static void DrawWiredEllipse(Vector3 position, Quaternion rotation, float width, float height, Color ellipseColor, Color wireColor)
        {
            DrawEllipse(position, rotation, width, height, ellipseColor);
            DrawWireEllipse(position, rotation, width, height, wireColor);
        }

        internal static void DrawArrow(Vector3 start, Vector3 end, float tipPivot, float shaftWidth, float tipWidth, Color color)
        {
            tipPivot = Mathf.Clamp(tipPivot, 0f, 1f);
            Vector3 pivot = start + tipPivot * (end - start);
            DrawLine(start, pivot, shaftWidth, color);
            DrawLine(pivot, end, tipWidth, 0f, color);
        }

        internal static void DrawArrow(Vector3 start, Vector3 end, float tipPivot, float shaftWidth, float tipWidth, Color shaftColor, Color tipColor)
        {
            tipPivot = Mathf.Clamp(tipPivot, 0f, 1f);
            Vector3 pivot = start + tipPivot * (end - start);
            DrawLine(start, pivot, shaftWidth, shaftColor);
            DrawLine(pivot, end, tipWidth, 0f, tipColor);
        }

        internal static void DrawGrid(Vector3 center, Quaternion rotation, int cellsX, int cellsY, float sizeX, float sizeY, Color color)
        {
            if (Return())
            {
                return;
            }

            float width = cellsX * sizeX;
            float height = cellsY * sizeY;
            Vector3 start = center - width / 2f * (rotation * Vector3.right) - height / 2f * (rotation * Vector3.forward);
            Vector3 dirX = rotation * Vector3.right;
            Vector3 dirY = rotation * Vector3.forward;
            for (int i = 0; i < cellsX + 1; i++)
            {
                DrawLine(start + i * sizeX * dirX, start + i * sizeX * dirX + height * dirY, color);
            }

            for (int i = 0; i < cellsY + 1; i++)
            {
                DrawLine(start + i * sizeY * dirY, start + i * sizeY * dirY + width * dirX, color);
            }
        }

        internal static void DrawQuad(Vector3 position, Quaternion rotation, float width, float height, Color color)
        {
            DrawMesh(QuadMesh, position, rotation, new Vector3(width, height, 1f), color);
        }

        internal static void DrawWireQuad(Vector3 position, Quaternion rotation, float width, float height, Color color)
        {
            DrawWire(QuadWire, position, rotation, new Vector3(width, height, 1f), color);
        }

        internal static void DrawWiredQuad(Vector3 position, Quaternion rotation, float width, float height, Color quadColor, Color wireColor)
        {
            DrawQuad(position, rotation, width, height, quadColor);
            DrawWireQuad(position, rotation, width, height, wireColor);
        }

        internal static void DrawCube(Vector3 position, Quaternion rotation, float size, Color color)
        {
            DrawMesh(CubeMesh, position, rotation, size * Vector3.one, color);
        }

        internal static void DrawWireCube(Vector3 position, Quaternion rotation, float size, Color color)
        {
            DrawWire(CubeWire, position, rotation, size * Vector3.one, color);
        }

        internal static void DrawWiredCube(Vector3 position, Quaternion rotation, float size, Color cubeColor, Color wireColor)
        {
            DrawCube(position, rotation, size, cubeColor);
            DrawWireCube(position, rotation, size, wireColor);
        }

        internal static void DrawCuboid(Vector3 position, Quaternion rotation, Vector3 size, Color color)
        {
            DrawMesh(CubeMesh, position, rotation, size, color);
        }

        internal static void DrawWireCuboid(Vector3 position, Quaternion rotation, Vector3 size, Color color)
        {
            DrawWire(CubeWire, position, rotation, size, color);
        }

        internal static void DrawWiredCuboid(Vector3 position, Quaternion rotation, Vector3 size, Color cuboidColor, Color wireColor)
        {
            DrawCuboid(position, rotation, size, cuboidColor);
            DrawWireCuboid(position, rotation, size, wireColor);
        }

        internal static void DrawSphere(Vector3 position, Quaternion rotation, float size, Color color)
        {
            DrawMesh(SphereMesh, position, rotation, size * Vector3.one, color);
        }

        internal static void DrawWireSphere(Vector3 position, Quaternion rotation, float size, Color color)
        {
            DrawWire(SphereWire, position, rotation, size * Vector3.one, color);
        }

        internal static void DrawWiredSphere(Vector3 position, Quaternion rotation, float size, Color sphereColor, Color wireColor)
        {
            DrawSphere(position, rotation, size, sphereColor);
            DrawWireSphere(position, rotation, size, wireColor);
        }

        internal static void DrawEllipsoid(Vector3 position, Quaternion rotation, float width, float height, Color color)
        {
            DrawMesh(SphereMesh, position, rotation, new Vector3(width, height, width), color);
        }

        internal static void DrawWireEllipsoid(Vector3 position, Quaternion rotation, float width, float height, Color color)
        {
            DrawWire(SphereWire, position, rotation, new Vector3(width, height, width), color);
        }

        internal static void DrawWiredEllipsoid(Vector3 position, Quaternion rotation, float width, float height, Color ellipsoidColor, Color wireColor)
        {
            DrawEllipsoid(position, rotation, width, height, ellipsoidColor);
            DrawWireEllipsoid(position, rotation, width, height, wireColor);
        }

        internal static void DrawCylinder(Vector3 position, Quaternion rotation, float width, float height, Color color)
        {
            DrawMesh(CylinderMesh, position, rotation, new Vector3(width, height / 2f, width), color);
        }

        internal static void DrawWireCylinder(Vector3 position, Quaternion rotation, float width, float height, Color color)
        {
            DrawWire(CylinderWire, position, rotation, new Vector3(width, height / 2f, width), color);
        }

        internal static void DrawWiredCylinder(Vector3 position, Quaternion rotation, float width, float height, Color cylinderColor, Color wireColor)
        {
            DrawCylinder(position, rotation, width, height, cylinderColor);
            DrawWireCylinder(position, rotation, width, height, wireColor);
        }

        internal static void DrawCapsule(Vector3 position, Quaternion rotation, float width, float height, Color color)
        {
            DrawMesh(CapsuleMesh, position, rotation, new Vector3(width, height / 2f, width), color);
        }

        internal static void DrawWireCapsule(Vector3 position, Quaternion rotation, float width, float height, Color color)
        {
            DrawWire(CapsuleWire, position, rotation, new Vector3(width, height / 2f, width), color);
        }

        internal static void DrawWiredCapsule(Vector3 position, Quaternion rotation, float width, float height, Color capsuleColor, Color wireColor)
        {
            DrawCapsule(position, rotation, width, height, capsuleColor);
            DrawWireCapsule(position, rotation, width, height, wireColor);
        }

        internal static void DrawCone(Vector3 position, Quaternion rotation, float width, float height, Color color)
        {
            DrawMesh(ConeMesh, position, rotation, new Vector3(width, height, width), color);
        }

        internal static void DrawWireCone(Vector3 position, Quaternion rotation, float width, float height, Color color)
        {
            DrawWire(ConeWire, position, rotation, new Vector3(width, height, width), color);
        }

        internal static void DrawWiredCone(Vector3 position, Quaternion rotation, float width, float height, Color coneColor, Color wireColor)
        {
            DrawCone(position, rotation, width, height, coneColor);
            DrawWireCone(position, rotation, width, height, wireColor);
        }

        internal static void DrawPyramid(Vector3 position, Quaternion rotation, float width, float height, Color color)
        {
            DrawMesh(PyramidMesh, position, rotation, new Vector3(width, height, width), color);
        }

        internal static void DrawWirePyramid(Vector3 position, Quaternion rotation, float width, float height, Color color)
        {
            DrawWire(PyramidWire, position, rotation, new Vector3(width, height, width), color);
        }

        internal static void DrawWiredPyramid(Vector3 position, Quaternion rotation, float width, float height, Color pyramidColor, Color wireColor)
        {
            DrawPyramid(position, rotation, width, height, pyramidColor);
            DrawWirePyramid(position, rotation, width, height, wireColor);
        }

        internal static void DrawBone(Vector3 position, Quaternion rotation, float width, float length, Color color)
        {
            DrawMesh(BoneMesh, position, rotation, new Vector3(width, width, length), color);
        }

        internal static void DrawWireBone(Vector3 position, Quaternion rotation, float width, float length, Color color)
        {
            DrawWire(BoneWire, position, rotation, new Vector3(width, width, length), color);
        }

        internal static void DrawWiredBone(Vector3 position, Quaternion rotation, float width, float length, Color boneColor, Color wireColor)
        {
            DrawBone(position, rotation, width, length, boneColor);
            DrawWireBone(position, rotation, width, length, wireColor);
        }

        internal static void DrawTranslateGizmo(Vector3 position, Quaternion rotation, float size)
        {
            if (Return())
            {
                return;
            }

            DrawLine(position, position + 0.8f * size * (rotation * Vector3.right), Red);
            DrawCone(position + 0.8f * size * (rotation * Vector3.right), rotation * Quaternion.Euler(0f, 0f, -90f),
                0.15f * size, 0.2f * size, Red);
            DrawLine(position, position + 0.8f * size * (rotation * Vector3.up), Green);
            DrawCone(position + 0.8f * size * (rotation * Vector3.up), rotation * Quaternion.Euler(0f, 0f, 0f),
                0.15f * size, 0.2f * size, Green);
            DrawLine(position, position + 0.8f * size * (rotation * Vector3.forward), Blue);
            DrawCone(position + 0.8f * size * (rotation * Vector3.forward), rotation * Quaternion.Euler(90f, 0f, 0f),
                0.15f * size, 0.2f * size, Blue);
        }

        internal static void DrawRotateGizmo(Vector3 position, Quaternion rotation, float size)
        {
            if (Return())
            {
                return;
            }

            SetProgram(PROGRAM.NONE);
            DrawWireCircle(position, rotation * Quaternion.Euler(0f, 90f, 0f), 2f * size, Red);
            SetProgram(PROGRAM.NONE);
            DrawWireCircle(position, rotation * Quaternion.Euler(90f, 0f, 90f), 2f * size, Green);
            SetProgram(PROGRAM.NONE);
            DrawWireCircle(position, rotation * Quaternion.Euler(0f, 0f, 0f), 2f * size, Blue);
            SetProgram(PROGRAM.NONE);
        }

        internal static void DrawScaleGizmo(Vector3 position, Quaternion rotation, float size)
        {
            if (Return())
            {
                return;
            }

            DrawLine(position, position + 0.85f * size * (rotation * Vector3.right), Red);
            DrawCube(position + 0.925f * size * (rotation * Vector3.right), rotation, 0.15f, Red);
            DrawLine(position, position + 0.85f * size * (rotation * Vector3.up), Green);
            DrawCube(position + 0.925f * size * (rotation * Vector3.up), rotation, 0.15f, Green);
            DrawLine(position, position + 0.85f * size * (rotation * Vector3.forward), Blue);
            DrawCube(position + 0.925f * size * (rotation * Vector3.forward), rotation, 0.15f, Blue);
        }

        internal static void DrawMesh(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale, Color color)
        {
            if (Return())
            {
                return;
            }

            SetProgram(PROGRAM.NONE);
            MeshMaterial.color = color;
            MeshMaterial.SetPass(0);
            Graphics.DrawMeshNow(mesh, Matrix4x4.TRS(position, rotation, scale));
        }

        internal static void DrawGUILine(Vector2 start, Vector2 end, Color color)
        {
            if (Camera != Camera.main)
            {
                return;
            }

            if (Return())
            {
                return;
            }

            SetProgram(PROGRAM.LINES);
            GL.Color(color);
            start.x *= Screen.width;
            start.y *= Screen.height;
            end.x *= Screen.width;
            end.y *= Screen.height;
            GL.Vertex(Camera.ScreenToWorldPoint(new Vector3(start.x, start.y, Camera.nearClipPlane + GUIOffset)));
            GL.Vertex(Camera.ScreenToWorldPoint(new Vector3(end.x, end.y, Camera.nearClipPlane + GUIOffset)));
        }

        internal static void DrawGUILine(Vector2 start, Vector2 end, float thickness, Color color)
        {
            if (Camera != Camera.main)
            {
                return;
            }

            if (Return())
            {
                return;
            }

            SetProgram(PROGRAM.QUADS);
            GL.Color(color);
            start.x *= Screen.width;
            start.y *= Screen.height;
            end.x *= Screen.width;
            end.y *= Screen.height;
            thickness *= Screen.width;
            Vector3 p1 = new Vector3(start.x, start.y, Camera.nearClipPlane + GUIOffset);
            Vector3 p2 = new Vector3(end.x, end.y, Camera.nearClipPlane + GUIOffset);
            Vector3 dir = end - start;
            Vector3 ortho = thickness / 2f * (Quaternion.AngleAxis(90f, Vector3.forward) * dir).normalized;
            GL.Vertex(Camera.ScreenToWorldPoint(p1 - ortho));
            GL.Vertex(Camera.ScreenToWorldPoint(p1 + ortho));
            GL.Vertex(Camera.ScreenToWorldPoint(p2 + ortho));
            GL.Vertex(Camera.ScreenToWorldPoint(p2 - ortho));
        }

        internal static void DrawGUIRectangle(Vector2 center, Vector2 size, Color color)
        {
            if (Camera != Camera.main)
            {
                return;
            }

            if (Return())
            {
                return;
            }

            SetProgram(PROGRAM.QUADS);
            GL.Color(color);
            center.x *= Screen.width;
            center.y *= Screen.height;
            size.x *= Screen.width;
            size.y *= Screen.height;
            GL.Vertex(Camera.ScreenToWorldPoint(new Vector3(center.x + size.x / 2f, center.y - size.y / 2f,
                Camera.nearClipPlane + GUIOffset)));
            GL.Vertex(Camera.ScreenToWorldPoint(new Vector3(center.x - size.x / 2f, center.y - size.y / 2f,
                Camera.nearClipPlane + GUIOffset)));
            GL.Vertex(Camera.ScreenToWorldPoint(new Vector3(center.x + -size.x / 2f, center.y + size.y / 2f,
                Camera.nearClipPlane + GUIOffset)));
            GL.Vertex(Camera.ScreenToWorldPoint(new Vector3(center.x + size.x / 2f, center.y + size.y / 2f,
                Camera.nearClipPlane + GUIOffset)));
        }

        internal static void DrawGUITriangle(Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            if (Camera != Camera.main)
            {
                return;
            }

            if (Return())
            {
                return;
            }

            SetProgram(PROGRAM.TRIANGLES);
            GL.Color(color);
            a.x *= Screen.width;
            a.y *= Screen.height;
            b.x *= Screen.width;
            b.y *= Screen.height;
            c.x *= Screen.width;
            c.y *= Screen.height;
            GL.Vertex(Camera.ScreenToWorldPoint(new Vector3(a.x, a.y, Camera.nearClipPlane + GUIOffset)));
            GL.Vertex(Camera.ScreenToWorldPoint(new Vector3(b.x, b.y, Camera.nearClipPlane + GUIOffset)));
            GL.Vertex(Camera.ScreenToWorldPoint(new Vector3(c.x, c.y, Camera.nearClipPlane + GUIOffset)));
        }

        internal static void DrawGUICircle(Vector2 center, float size, Color color)
        {
            if (Camera != Camera.main)
            {
                return;
            }

            if (Return())
            {
                return;
            }

            SetProgram(PROGRAM.TRIANGLE_STRIP);
            GL.Color(color);
            center.x *= Screen.width;
            center.y *= Screen.height;
            for (int i = 0; i < CircleWire.Length; i++)
            {
                GL.Vertex(Camera.ScreenToWorldPoint(new Vector3(center.x + size * CircleWire[i].x * Screen.width,
                    center.y + size * CircleWire[i].y * Screen.width, Camera.nearClipPlane + GUIOffset)));
                GL.Vertex(Camera.ScreenToWorldPoint(new Vector3(center.x, center.y, Camera.nearClipPlane + GUIOffset)));
            }
        }

        internal static void DrawGUIFunction(Vector2 center, Vector2 size, float[] values, float yMin, float yMax, Color background, Color line)
        {
            DrawGUIRectangle(center, size, background);
            float x = center.x - size.x / 2f;
            float y = center.y - size.y / 2f;
            float scale = yMax - yMin;
            for (int i = 0; i < values.Length - 1; i++)
            {
                DrawGUILine(
                    new Vector2(x + (float)i / (float)(values.Length - 1) * size.x,
                        y + Mathf.Clamp(values[i] / scale, 0f, 1f) * size.y),
                    new Vector2(x + (float)(i + 1) / (float)(values.Length - 1) * size.x,
                        y + Mathf.Clamp(values[i + 1] / scale, 0f, 1f) * size.y),
                    line
                );
            }
        }

        internal static void DrawGUIFunction(Vector2 center, Vector2 size, float[] values, float yMin, float yMax, float thickness, Color background, Color line)
        {
            DrawGUIRectangle(center, size, background);
            float x = center.x - size.x / 2f;
            float y = center.y - size.y / 2f;
            float scale = yMax - yMin;
            for (int i = 0; i < values.Length - 1; i++)
            {
                DrawGUILine(
                    new Vector2(x + (float)i / (float)(values.Length - 1) * size.x,
                        y + Mathf.Clamp(values[i] / scale, 0f, 1f) * size.y),
                    new Vector2(x + (float)(i + 1) / (float)(values.Length - 1) * size.x,
                        y + Mathf.Clamp(values[i + 1] / scale, 0f, 1f) * size.y),
                    thickness,
                    line
                );
            }
        }

        internal static void DrawGUIFunctions(Vector2 center, Vector2 size, List<float[]> values, float yMin, float yMax, Color background, Color[] lines)
        {
            DrawGUIRectangle(center, size, background);
            float x = center.x - size.x / 2f;
            float y = center.y - size.y / 2f;
            float scale = yMax - yMin;
            for (int k = 0; k < values.Count; k++)
            {
                for (int i = 0; i < values[k].Length - 1; i++)
                {
                    DrawGUILine(
                        new Vector2(x + (float)i / (float)(values[k].Length - 1) * size.x,
                            y + Mathf.Clamp(values[k][i] / scale, 0f, 1f) * size.y),
                        new Vector2(x + (float)(i + 1) / (float)(values[k].Length - 1) * size.x,
                            y + Mathf.Clamp(values[k][i + 1] / scale, 0f, 1f) * size.y),
                        lines[k]
                    );
                }
            }
        }

        internal static void DrawGUIFunctions(Vector2 center, Vector2 size, List<float[]> values, float yMin, float yMax, float thickness, Color background, Color[] lines)
        {
            DrawGUIRectangle(center, size, background);
            float x = center.x - size.x / 2f;
            float y = center.y - size.y / 2f;
            float scale = yMax - yMin;
            for (int k = 0; k < values.Count; k++)
            {
                for (int i = 0; i < values[k].Length - 1; i++)
                {
                    DrawGUILine(
                        new Vector2(x + (float)i / (float)(values[k].Length - 1) * size.x,
                            y + Mathf.Clamp(values[k][i] / scale, 0f, 1f) * size.y),
                        new Vector2(x + (float)(i + 1) / (float)(values[k].Length - 1) * size.x,
                            y + Mathf.Clamp(values[k][i + 1] / scale, 0f, 1f) * size.y),
                        thickness,
                        lines[k]
                    );
                }
            }
        }

        private static bool Return()
        {
            if (!Active)
            {
                Debug.Log("Drawing is not active. Call 'Begin()' first.");
            }

            return !Active;
        }

        static void Initialize()
        {
            if (Initialized != null)
            {
                return;
            }

            Resources.UnloadUnusedAssets();

            GLMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
            GLMaterial.hideFlags = HideFlags.HideAndDontSave;
            GLMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            GLMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            GLMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Back);
            GLMaterial.SetInt("_ZWrite", 1);
            GLMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);

            MeshMaterial = new Material(Shader.Find("DebugDraw"));
            MeshMaterial.hideFlags = HideFlags.HideAndDontSave;
            MeshMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Back);
            MeshMaterial.SetInt("_ZWrite", 1);
            MeshMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
            MeshMaterial.SetFloat("_Power", 0.25f);

            CircleMesh = CreateCircleMesh(Resolution);
            QuadMesh = GetPrimitiveMesh(PrimitiveType.Quad);
            CubeMesh = GetPrimitiveMesh(PrimitiveType.Cube);
            SphereMesh = GetPrimitiveMesh(PrimitiveType.Sphere);
            CylinderMesh = GetPrimitiveMesh(PrimitiveType.Cylinder);
            CapsuleMesh = GetPrimitiveMesh(PrimitiveType.Capsule);
            ConeMesh = CreateConeMesh(Resolution);
            PyramidMesh = CreatePyramidMesh();
            BoneMesh = CreateBoneMesh();

            CircleWire = CreateCircleWire(Resolution);
            QuadWire = CreateQuadWire();
            CubeWire = CreateCubeWire();
            SphereWire = CreateSphereWire(Resolution);
            CylinderWire = CreateCylinderWire(Resolution);
            CapsuleWire = CreateCapsuleWire(Resolution);
            ConeWire = CreateConeWire(Resolution);
            PyramidWire = CreatePyramidWire();
            BoneWire = CreateBoneWire();

            Initialized = new Mesh();
        }

        private static void SetProgram(PROGRAM program)
        {
            if (Program != program)
            {
                Program = program;
                GL.End();
                if (Program != PROGRAM.NONE)
                {
                    GLMaterial.SetPass(0);
                    switch (Program)
                    {
                        case PROGRAM.LINES:
                            GL.Begin(GL.LINES);
                            break;
                        case PROGRAM.TRIANGLES:
                            GL.Begin(GL.TRIANGLES);
                            break;
                        case PROGRAM.TRIANGLE_STRIP:
                            GL.Begin(GL.TRIANGLE_STRIP);
                            break;
                        case PROGRAM.QUADS:
                            GL.Begin(GL.QUADS);
                            break;
                    }
                }
            }
        }

        private static void DrawWire(Vector3[] points, Vector3 position, Quaternion rotation, Vector3 scale, Color color)
        {
            if (Return())
            {
                return;
            }

            SetProgram(PROGRAM.LINES);
            GL.Color(color);
            for (int i = 0; i < points.Length; i += 2)
            {
                GL.Vertex(position + rotation * Vector3.Scale(scale, points[i]));
                GL.Vertex(position + rotation * Vector3.Scale(scale, points[i + 1]));
            }
        }

        private static Camera GetCamera()
        {
            if (Camera.current != null)
            {
                return Camera.current;
            }
            else
            {
                return Camera.main;
            }
        }

        private static Mesh GetPrimitiveMesh(PrimitiveType type)
        {
            GameObject gameObject = GameObject.CreatePrimitive(type);
            gameObject.hideFlags = HideFlags.HideInHierarchy;
            gameObject.GetComponent<MeshRenderer>().enabled = false;
            Mesh mesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
            if (Application.isPlaying)
            {
                GameObject.Destroy(gameObject);
            }
            else
            {
                GameObject.DestroyImmediate(gameObject);
            }

            return mesh;
        }

        private static Mesh CreateCircleMesh(int resolution)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            float step = 360.0f / (float)resolution;
            Quaternion quaternion = Quaternion.Euler(0f, 0f, step);
            vertices.Add(new Vector3(0f, 0f, 0f));
            vertices.Add(new Vector3(0f, 0.5f, 0f));
            vertices.Add(quaternion * vertices[1]);
            triangles.Add(1);
            triangles.Add(0);
            triangles.Add(2);
            for (int i = 0; i < resolution - 1; i++)
            {
                triangles.Add(vertices.Count - 1);
                triangles.Add(0);
                triangles.Add(vertices.Count);
                vertices.Add(quaternion * vertices[vertices.Count - 1]);
            }

            Mesh mesh = new Mesh();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            return mesh;
        }

        private static Mesh CreateConeMesh(int resolution)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            float step = 360.0f / (float)resolution;
            Quaternion quaternion = Quaternion.Euler(0f, step, 0f);
            vertices.Add(new Vector3(0f, 1f, 0f));
            vertices.Add(new Vector3(0f, 0f, 0f));
            vertices.Add(new Vector3(0f, 0f, 0.5f));
            vertices.Add(quaternion * vertices[2]);
            triangles.Add(2);
            triangles.Add(1);
            triangles.Add(3);
            triangles.Add(2);
            triangles.Add(3);
            triangles.Add(0);
            for (int i = 0; i < resolution - 1; i++)
            {
                triangles.Add(vertices.Count - 1);
                triangles.Add(1);
                triangles.Add(vertices.Count);
                triangles.Add(vertices.Count - 1);
                triangles.Add(vertices.Count);
                triangles.Add(0);
                vertices.Add(quaternion * vertices[vertices.Count - 1]);
            }

            Mesh mesh = new Mesh();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            return mesh;
        }

        private static Mesh CreatePyramidMesh()
        {
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            vertices.Add(new Vector3(-0.5f, 0f, -0.5f));
            vertices.Add(new Vector3(0.5f, 0f, -0.5f));
            vertices.Add(new Vector3(0.5f, 0f, 0.5f));
            vertices.Add(new Vector3(-0.5f, 0f, -0.5f));
            vertices.Add(new Vector3(0.5f, 0f, 0.5f));
            vertices.Add(new Vector3(-0.5f, 0f, 0.5f));
            vertices.Add(new Vector3(-0.5f, 0f, -0.5f));
            vertices.Add(new Vector3(0f, 1f, 0f));
            vertices.Add(new Vector3(0.5f, 0f, -0.5f));
            vertices.Add(new Vector3(0.5f, 0f, -0.5f));
            vertices.Add(new Vector3(0f, 1f, 0f));
            vertices.Add(new Vector3(0.5f, 0f, 0.5f));
            vertices.Add(new Vector3(0.5f, 0f, 0.5f));
            vertices.Add(new Vector3(0f, 1f, 0f));
            vertices.Add(new Vector3(-0.5f, 0f, 0.5f));
            vertices.Add(new Vector3(-0.5f, 0f, 0.5f));
            vertices.Add(new Vector3(0f, 1f, 0f));
            vertices.Add(new Vector3(-0.5f, 0f, -0.5f));
            for (int i = 0; i < 18; i++)
            {
                triangles.Add(i);
            }

            Mesh mesh = new Mesh();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            return mesh;
        }

        private static Mesh CreateBoneMesh()
        {
            float size = 1f / 7f;
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            vertices.Add(new Vector3(-size, -size, 0.200f));
            vertices.Add(new Vector3(-size, size, 0.200f));
            vertices.Add(new Vector3(0.000f, 0.000f, 0.000f));
            vertices.Add(new Vector3(size, size, 0.200f));
            vertices.Add(new Vector3(0.000f, 0.000f, 1.000f));
            vertices.Add(new Vector3(size, -size, 0.200f));
            vertices.Add(new Vector3(-size, size, 0.200f));
            vertices.Add(new Vector3(-size, -size, 0.200f));
            vertices.Add(new Vector3(0.000f, 0.000f, 1.000f));
            vertices.Add(new Vector3(size, -size, 0.200f));
            vertices.Add(new Vector3(0.000f, 0.000f, 1.000f));
            vertices.Add(new Vector3(-size, -size, 0.200f));
            vertices.Add(new Vector3(size, size, 0.200f));
            vertices.Add(new Vector3(-size, size, 0.200f));
            vertices.Add(new Vector3(0.000f, 0.000f, 1.000f));
            vertices.Add(new Vector3(size, size, 0.200f));
            vertices.Add(new Vector3(size, -size, 0.200f));
            vertices.Add(new Vector3(0.000f, 0.000f, 0.000f));
            vertices.Add(new Vector3(size, size, 0.200f));
            vertices.Add(new Vector3(0.000f, 0.000f, 0.000f));
            vertices.Add(new Vector3(-size, size, 0.200f));
            vertices.Add(new Vector3(size, -size, 0.200f));
            vertices.Add(new Vector3(-size, -size, 0.200f));
            vertices.Add(new Vector3(0.000f, 0.000f, 0.000f));
            for (int i = 0; i < 24; i++)
            {
                triangles.Add(i);
            }

            Mesh mesh = new Mesh();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.RecalculateNormals();
            return mesh;
        }

        private static Vector3[] CreateCircleWire(int resolution)
        {
            List<Vector3> points = new List<Vector3>();
            float step = 360.0f / (float)resolution;
            for (int i = 0; i < resolution; i++)
            {
                points.Add(Quaternion.Euler(0f, 0f, i * step) * new Vector3(0f, 0.5f, 0f));
                points.Add(Quaternion.Euler(0f, 0f, (i + 1) * step) * new Vector3(0f, 0.5f, 0f));
            }

            return points.ToArray();
        }

        private static Vector3[] CreateQuadWire()
        {
            List<Vector3> points = new List<Vector3>();
            points.Add(new Vector3(-0.5f, -0.5f, 0f));
            points.Add(new Vector3(0.5f, -0.5f, 0f));

            points.Add(new Vector3(0.5f, -0.5f, 0f));
            points.Add(new Vector3(0.5f, 0.5f, 0f));

            points.Add(new Vector3(0.5f, 0.5f, 0f));
            points.Add(new Vector3(-0.5f, 0.5f, 0f));

            points.Add(new Vector3(-0.5f, 0.5f, 0f));
            points.Add(new Vector3(-0.5f, -0.5f, 0f));
            return points.ToArray();
        }

        private static Vector3[] CreateCubeWire()
        {
            float size = 1f;
            Vector3 A = new Vector3(-size / 2f, -size / 2f, -size / 2f);
            Vector3 B = new Vector3(size / 2f, -size / 2f, -size / 2f);
            Vector3 C = new Vector3(-size / 2f, -size / 2f, size / 2f);
            Vector3 D = new Vector3(size / 2f, -size / 2f, size / 2f);
            Vector3 p1 = A;
            Vector3 p2 = B;
            Vector3 p3 = C;
            Vector3 p4 = D;
            Vector3 p5 = -D;
            Vector3 p6 = -C;
            Vector3 p7 = -B;
            Vector3 p8 = -A;
            List<Vector3> points = new List<Vector3>();
            points.Add(p1);
            points.Add(p2);
            points.Add(p2);
            points.Add(p4);
            points.Add(p4);
            points.Add(p3);
            points.Add(p3);
            points.Add(p1);
            points.Add(p5);
            points.Add(p6);
            points.Add(p6);
            points.Add(p8);
            points.Add(p5);
            points.Add(p7);
            points.Add(p7);
            points.Add(p8);
            points.Add(p1);
            points.Add(p5);
            points.Add(p2);
            points.Add(p6);
            points.Add(p3);
            points.Add(p7);
            points.Add(p4);
            points.Add(p8);
            return points.ToArray();
        }

        private static Vector3[] CreateSphereWire(int resolution)
        {
            List<Vector3> points = new List<Vector3>();
            float step = 360.0f / (float)resolution;
            for (int i = 0; i < resolution; i++)
            {
                points.Add(Quaternion.Euler(0f, 0f, i * step) * new Vector3(0f, 0.5f, 0f));
                points.Add(Quaternion.Euler(0f, 0f, (i + 1) * step) * new Vector3(0f, 0.5f, 0f));
            }

            for (int i = 0; i < resolution; i++)
            {
                points.Add(Quaternion.Euler(0f, i * step, 0f) * new Vector3(0f, 0f, 0.5f));
                points.Add(Quaternion.Euler(0f, (i + 1) * step, 0f) * new Vector3(0f, 0f, 0.5f));
            }

            for (int i = 0; i < resolution; i++)
            {
                points.Add(Quaternion.Euler(i * step, 0f, i * step) * new Vector3(0f, 0f, 0.5f));
                points.Add(Quaternion.Euler((i + 1) * step, 0f, (i + 1) * step) * new Vector3(0f, 0f, 0.5f));
            }

            return points.ToArray();
        }

        private static Vector3[] CreateCylinderWire(int resolution)
        {
            List<Vector3> points = new List<Vector3>();
            float step = 360.0f / (float)resolution;
            for (int i = 0; i < resolution; i++)
            {
                points.Add(Quaternion.Euler(0f, i * step, 0f) * new Vector3(0f, 0f, 0.5f) + new Vector3(0f, 1f, 0f));
                points.Add(Quaternion.Euler(0f, (i + 1) * step, 0f) * new Vector3(0f, 0f, 0.5f) + new Vector3(0f, 1f, 0f));
            }

            for (int i = 0; i < resolution; i++)
            {
                points.Add(Quaternion.Euler(0f, i * step, 0f) * new Vector3(0f, 0f, 0.5f) - new Vector3(0f, 1f, 0f));
                points.Add(Quaternion.Euler(0f, (i + 1) * step, 0f) * new Vector3(0f, 0f, 0.5f) - new Vector3(0f, 1f, 0f));
            }

            points.Add(new Vector3(0f, -1f, -0.5f));
            points.Add(new Vector3(0f, 1f, -0.5f));
            points.Add(new Vector3(0f, -1f, 0.5f));
            points.Add(new Vector3(0f, 1f, 0.5f));
            points.Add(new Vector3(-0.5f, -1f, 0f));
            points.Add(new Vector3(-0.5f, 1f, 0f));
            points.Add(new Vector3(0.5f, -1f, 0f));
            points.Add(new Vector3(0.5f, 1f, 0));
            return points.ToArray();
        }

        private static Vector3[] CreateCapsuleWire(int resolution)
        {
            List<Vector3> points = new List<Vector3>();
            float step = 360.0f / (float)resolution;
            for (int i = -resolution / 4 - 1; i <= resolution / 4; i++)
            {
                points.Add(Quaternion.Euler(0f, 0f, i * step) * new Vector3(0f, 0.5f, 0f) + new Vector3(0f, 0.5f, 0f));
                points.Add(Quaternion.Euler(0f, 0f, (i + 1) * step) * new Vector3(0f, 0.5f, 0f) +
                    new Vector3(0f, 0.5f, 0f));
            }

            for (int i = resolution / 2; i < resolution; i++)
            {
                points.Add(Quaternion.Euler(i * step, 0f, i * step) * new Vector3(0f, 0f, 0.5f) +
                    new Vector3(0f, 0.5f, 0f));
                points.Add(Quaternion.Euler((i + 1) * step, 0f, (i + 1) * step) * new Vector3(0f, 0f, 0.5f) +
                    new Vector3(0f, 0.5f, 0f));
            }

            for (int i = -resolution / 4 - 1; i <= resolution / 4; i++)
            {
                points.Add(Quaternion.Euler(0f, 0f, i * step) * new Vector3(0f, -0.5f, 0f) + new Vector3(0f, -0.5f, 0f));
                points.Add(Quaternion.Euler(0f, 0f, (i + 1) * step) * new Vector3(0f, -0.5f, 0f) +
                    new Vector3(0f, -0.5f, 0f));
            }

            for (int i = resolution / 2; i < resolution; i++)
            {
                points.Add(Quaternion.Euler(i * step, 0f, i * step) * new Vector3(0f, 0f, -0.5f) +
                    new Vector3(0f, -0.5f, 0f));
                points.Add(Quaternion.Euler((i + 1) * step, 0f, (i + 1) * step) * new Vector3(0f, 0f, -0.5f) +
                    new Vector3(0f, -0.5f, 0f));
            }

            points.Add(new Vector3(0f, -0.5f, -0.5f));
            points.Add(new Vector3(0f, 0.5f, -0.5f));
            points.Add(new Vector3(0f, -0.5f, 0.5f));
            points.Add(new Vector3(0f, 0.5f, 0.5f));
            points.Add(new Vector3(-0.5f, -0.5f, 0f));
            points.Add(new Vector3(-0.5f, 0.5f, 0f));
            points.Add(new Vector3(0.5f, -0.5f, 0f));
            points.Add(new Vector3(0.5f, 0.5f, 0));
            return points.ToArray();
        }

        private static Vector3[] CreateConeWire(int resolution)
        {
            List<Vector3> points = new List<Vector3>();
            float step = 360.0f / (float)resolution;
            for (int i = 0; i < resolution; i++)
            {
                points.Add(Quaternion.Euler(0f, i * step, 0f) * new Vector3(0f, 0f, 0.5f));
                points.Add(Quaternion.Euler(0f, (i + 1) * step, 0f) * new Vector3(0f, 0f, 0.5f));
            }

            points.Add(new Vector3(-0.5f, 0f, 0f));
            points.Add(new Vector3(0f, 1f, 0f));
            points.Add(new Vector3(0.5f, 0f, 0f));
            points.Add(new Vector3(0f, 1f, 0f));
            points.Add(new Vector3(0f, 0f, -0.5f));
            points.Add(new Vector3(0f, 1f, 0f));
            points.Add(new Vector3(0f, 0f, 0.5f));
            points.Add(new Vector3(0f, 1f, 0f));
            return points.ToArray();
        }

        private static Vector3[] CreatePyramidWire()
        {
            List<Vector3> points = new List<Vector3>();
            points.Add(new Vector3(-0.5f, 0f, -0.5f));
            points.Add(new Vector3(0.5f, 0f, -0.5f));
            points.Add(new Vector3(0.5f, 0f, -0.5f));
            points.Add(new Vector3(0.5f, 0f, 0.5f));
            points.Add(new Vector3(0.5f, 0f, 0.5f));
            points.Add(new Vector3(-0.5f, 0f, 0.5f));
            points.Add(new Vector3(-0.5f, 0f, 0.5f));
            points.Add(new Vector3(-0.5f, 0f, -0.5f));
            points.Add(new Vector3(-0.5f, 0f, -0.5f));
            points.Add(new Vector3(0f, 1f, 0f));
            points.Add(new Vector3(0.5f, 0f, -0.5f));
            points.Add(new Vector3(0f, 1f, 0f));
            points.Add(new Vector3(-0.5f, 0f, 0.5f));
            points.Add(new Vector3(0f, 1f, 0f));
            points.Add(new Vector3(0.5f, 0f, 0.5f));
            points.Add(new Vector3(0f, 1f, 0f));
            return points.ToArray();
        }

        private static Vector3[] CreateBoneWire()
        {
            float size = 1f / 7f;
            List<Vector3> points = new List<Vector3>();
            points.Add(new Vector3(0.000f, 0.000f, 0.000f));
            points.Add(new Vector3(-size, -size, 0.200f));
            points.Add(new Vector3(0.000f, 0.000f, 0.000f));
            points.Add(new Vector3(size, -size, 0.200f));
            points.Add(new Vector3(0.000f, 0.000f, 0.000f));
            points.Add(new Vector3(-size, size, 0.200f));
            points.Add(new Vector3(0.000f, 0.000f, 0.000f));
            points.Add(new Vector3(size, size, 0.200f));
            points.Add(new Vector3(-size, -size, 0.200f));
            points.Add(new Vector3(0.000f, 0.000f, 1.000f));
            points.Add(new Vector3(size, -size, 0.200f));
            points.Add(new Vector3(0.000f, 0.000f, 1.000f));
            points.Add(new Vector3(-size, size, 0.200f));
            points.Add(new Vector3(0.000f, 0.000f, 1.000f));
            points.Add(new Vector3(size, size, 0.200f));
            points.Add(new Vector3(0.000f, 0.000f, 1.000f));
            points.Add(new Vector3(-size, -size, 0.200f));
            points.Add(new Vector3(size, -size, 0.200f));
            points.Add(new Vector3(size, -size, 0.200f));
            points.Add(new Vector3(size, size, 0.200f));
            points.Add(new Vector3(size, size, 0.200f));
            points.Add(new Vector3(-size, size, 0.200f));
            points.Add(new Vector3(-size, size, 0.200f));
            points.Add(new Vector3(-size, -size, 0.200f));
            return points.ToArray();
        }

        internal static Color Transparent(this Color color, float opacity)
        {
            return new Color(color.r, color.g, color.b, Mathf.Clamp(opacity, 0f, 1f));
        }

        internal static Color[] GetRainbowColors(int number)
        {
            Color[] colors = new Color[number];
            for (int i = 0; i < number; i++)
            {
                float frequency = 5f / number;
                colors[i].r = Normalize(Mathf.Sin(frequency * i + 0f) * (127f) + 128f, 0f, 255f, 0f, 1f);
                colors[i].g = Normalize(Mathf.Sin(frequency * i + 2f) * (127f) + 128f, 0f, 255f, 0f, 1f);
                colors[i].b = Normalize(Mathf.Sin(frequency * i + 4f) * (127f) + 128f, 0f, 255f, 0f, 1f);
                colors[i].a = 1f;
            }

            return colors;
        }

        internal static float Normalize(float value, float valueMin, float valueMax, float resultMin, float resultMax)
        {
            if (valueMax - valueMin != 0f)
            {
                return (value - valueMin) / (valueMax - valueMin) * (resultMax - resultMin) + resultMin;
            }
            else
            {
                //Not possible to normalise input value.
                return value;
            }
        }

        private static int Resolution = 30;

        private static Mesh Initialized;

        private static bool Active;

        private static Material GLMaterial;
        private static Material MeshMaterial;

        private static float GUIOffset = 0.001f;

        private static Camera Camera;
        private static Vector3 ViewPosition;
        private static Quaternion ViewRotation;

        private static PROGRAM Program = PROGRAM.NONE;

        private enum PROGRAM
        {
            NONE,
            LINES,
            TRIANGLES,
            TRIANGLE_STRIP,
            QUADS
        };

        private static Mesh CircleMesh;
        private static Mesh QuadMesh;
        private static Mesh CubeMesh;
        private static Mesh SphereMesh;
        private static Mesh CylinderMesh;
        private static Mesh CapsuleMesh;
        private static Mesh ConeMesh;
        private static Mesh PyramidMesh;
        private static Mesh BoneMesh;

        private static Vector3[] CircleWire;
        private static Vector3[] QuadWire;
        private static Vector3[] CubeWire;
        private static Vector3[] SphereWire;
        private static Vector3[] CylinderWire;
        private static Vector3[] CapsuleWire;
        private static Vector3[] ConeWire;
        private static Vector3[] PyramidWire;
        private static Vector3[] BoneWire;
    }
}
