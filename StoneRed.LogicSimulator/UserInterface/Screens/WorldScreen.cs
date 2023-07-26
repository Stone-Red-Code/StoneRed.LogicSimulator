using FontStashSharp.RichText;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using MonoGame.Extended;
using MonoGame.Extended.Input;

using Myra.Graphics2D.UI;

using StoneRed.LogicSimulator.Simulation;
using StoneRed.LogicSimulator.Simulation.LogicGates.Interfaces;

using System;
using System.Collections.Generic;

using IColorable = StoneRed.LogicSimulator.Simulation.LogicGates.Interfaces.IColorable;

namespace StoneRed.LogicSimulator.UserInterface.Screens;

internal class WorldScreen : SrlsScreen<Grid>
{
    private readonly LogicGateSimulator simulator;
    private OrthographicCamera camera = null!;
    private Label fpsLabel = null!;
    private Label upsLabel = null!;
    private SpinButton frequency = null!;
    private CheckBox highPerformance = null!;
    private float fps;

    private MouseStateExtended previousMouseState;
    public override bool ScalingEnabled => false;

    protected override string XmmpPath => "WorldScreen.xmmp";

    public WorldScreen(List<LogicGate> logicGates)
    {
        simulator = new LogicGateSimulator(logicGates);
    }

    public WorldScreen()
    {
        simulator = new LogicGateSimulator(new List<LogicGate>());
    }

    protected override void Initialize()
    {
        VerticalStackPanel infoPanel = Root.FindChildById<VerticalStackPanel>("info");
        fpsLabel = infoPanel.FindChildById<Label>("fps");
        upsLabel = infoPanel.FindChildById<Label>("ups");

        VerticalStackPanel settingsPanel = Root.FindChildById<VerticalStackPanel>("settings");
        frequency = settingsPanel.FindChildById<SpinButton>("frequency");
        highPerformance = settingsPanel.FindChildById<CheckBox>("highPerformance");

        camera = new OrthographicCamera(srls.GraphicsDevice)
        {
            MinimumZoom = 0.1f,
            MaximumZoom = 2,
            Zoom = 0.5f,
        };

        simulator.Start();
    }

    protected override void Draw(GameTime gameTime)
    {
        fps = 1f / gameTime.GetElapsedSeconds();

        RichTextLayout richTextLayout = new RichTextLayout
        {
            Font = srls.FontSystem.GetFont(15)
        };

        Matrix transformMatrix = camera.GetViewMatrix();
        srls.SpriteBatch.Begin(SpriteSortMode.BackToFront, transformMatrix: transformMatrix);

        foreach (LogicGate logicGate in simulator.GetLogicGates())
        {
            Color color = Color.White;

            if (logicGate is IColorable colorable)
            {
                color = colorable.Color;
            }
            if (logicGate is IInteractable interactable)
            {
                richTextLayout.Text = interactable.Info;
                richTextLayout.Draw(srls.SpriteBatch, (new Vector2(0, 15) * srls.Scale) + (logicGate.Metadata.Position * srls.Scale), Color.Black, new Vector2(srls.Scale, srls.Scale), layerDepth: 0);
            }

            richTextLayout.Text = logicGate.Metadata.Name;

            richTextLayout.Draw(srls.SpriteBatch, logicGate.Metadata.Position * srls.Scale, Color.Black, new Vector2(srls.Scale, srls.Scale), layerDepth: 0);

            srls.SpriteBatch.FillRectangle(logicGate.Metadata.Position * srls.Scale, logicGate.Metadata.Size * srls.Scale, color, 0.2f);

            foreach (LogicGateConnection connection in logicGate.LogicGateConnections)
            {
                Color lineColor = logicGate.GetCachedOutputBit(connection.OutputIndex) == 1 ? Color.Red : Color.LightBlue;
                srls.SpriteBatch.DrawLine((logicGate.Metadata.Position * srls.Scale) + (logicGate.Metadata.Size / 2 * srls.Scale), (connection.LogicGate.Metadata.Position * srls.Scale) + (logicGate.Metadata.Size / 2 * srls.Scale), lineColor, 5 * srls.Scale, 0.1f);
            }
        }

        srls.SpriteBatch.End();
    }

    protected override void Update(GameTime gameTime)
    {
        double calibrationPercentage = 0;

        if (simulator.ClockCalibrating)
        {
            if (simulator.ActualTicksPerSecond > simulator.TargetTicksPerSecond)
            {
                calibrationPercentage = (double)simulator.TargetTicksPerSecond / simulator.ActualTicksPerSecond;
            }
            else if (simulator.TargetTicksPerSecond > simulator.ActualTicksPerSecond)
            {
                calibrationPercentage = (double)simulator.ActualTicksPerSecond / simulator.TargetTicksPerSecond;
            }

            calibrationPercentage = Math.Round(calibrationPercentage * 100, 2);
        }

        fpsLabel.Text = $"FPS: {Math.Round(fps)}";
        upsLabel.Text = $"FRQ: {CalculateFrequency(simulator.ActualTicksPerSecond)}/{CalculateFrequency(simulator.TargetTicksPerSecond)}{(simulator.HighPerformanceClock ? "*" : string.Empty)} {(simulator.ClockCalibrating ? $"[Calibrating... {calibrationPercentage}%]" : string.Empty)}";
        simulator.TargetTicksPerSecond = (int)frequency.Value.GetValueOrDefault();
        simulator.HighPerformanceClock = highPerformance.IsChecked;

        fpsLabel.Font = srls.FontSystem.GetFont(10 * srls.Scale);
        upsLabel.Font = srls.FontSystem.GetFont(10 * srls.Scale);

        MouseStateExtended mouseState = MouseExtended.GetState();
        KeyboardStateExtended keyboardState = KeyboardExtended.GetState();

        bool mouseOverGate = false;

        foreach (LogicGate logicGate in simulator.GetLogicGates())
        {
            Rectangle rectangle = new Rectangle((logicGate.Metadata.Position * srls.Scale).ToPoint(), (logicGate.Metadata.Size * srls.Scale).ToPoint());

            if (rectangle.Contains(camera.ScreenToWorld(mouseState.Position.ToVector2())))
            {
                if (mouseState.IsButtonDown(MouseButton.Right))
                {
                    ShowConnectionContextMenu(logicGate, mouseState.Position, keyboardState.IsShiftDown());
                }
                else if (!srls.Desktop.IsMouseOverGUI && logicGate is IInteractable interactable)
                {
                    interactable.OnInteraction(mouseState, previousMouseState, keyboardState);
                }

                mouseOverGate = true;
            }
        }

        if (!mouseOverGate && !srls.Desktop.IsMouseOverGUI)
        {
            srls.Desktop.HideContextMenu();
        }

        previousMouseState = mouseState;

        float movementSpeed = (float)Math.Pow(200, 2 - camera.Zoom);

        movementSpeed = Math.Clamp(movementSpeed, 1000, 20000);

        camera.Move(GetMovementDirection(keyboardState) * movementSpeed * gameTime.GetElapsedSeconds());

        if (!keyboardState.IsShiftDown())
        {
            if (mouseState.DeltaScrollWheelValue < 0)
            {
                camera.ZoomIn(0.1f);
            }
            else if (mouseState.DeltaScrollWheelValue > 0)
            {
                camera.ZoomOut(0.1f);
            }
        }
    }

    protected override void UnloadContent()
    {
        simulator.Stop();
    }

    private static Vector2 GetMovementDirection(KeyboardStateExtended keyboardState)
    {
        Vector2 movementDirection = Vector2.Zero;
        if (keyboardState.IsKeyDown(Keys.S))
        {
            movementDirection += Vector2.UnitY;
        }
        if (keyboardState.IsKeyDown(Keys.W))
        {
            movementDirection -= Vector2.UnitY;
        }
        if (keyboardState.IsKeyDown(Keys.A))
        {
            movementDirection -= Vector2.UnitX;
        }
        if (keyboardState.IsKeyDown(Keys.D))
        {
            movementDirection += Vector2.UnitX;
        }
        return movementDirection;
    }

    private static string CalculateFrequency(double hz)
    {
        double khz = hz / 1000d;
        double mhz = khz / 1000d;
        double ghz = mhz / 1000d;

        if (ghz >= 1)
        {
            return $"{Math.Round(ghz)}GHz";
        }
        else if (mhz >= 1)
        {
            return $"{Math.Round(mhz)}MHz";
        }
        else if (khz >= 1)
        {
            return $"{Math.Round(khz)}kHz";
        }
        else
        {
            return $"{Math.Round(hz)}Hz";
        }
    }

    private void ShowConnectionContextMenu(LogicGate logicGate, Point position, bool showInputs)
    {
        showInputs = logicGate.OutputCount <= 0 || (showInputs && logicGate.InputCount > 0);
        int count = showInputs ? logicGate.InputCount : logicGate.OutputCount;

        MenuItem[] menuItems = new MenuItem[count];

        for (int i = 0; i < count; i++)
        {
            menuItems[i] = new MenuItem(i.ToString(), showInputs ? $"Input {i}" : $"Output {i}");
        }

        srls.ShowContextMenu(logicGate.Metadata.Name, position, menuItems);
    }
}