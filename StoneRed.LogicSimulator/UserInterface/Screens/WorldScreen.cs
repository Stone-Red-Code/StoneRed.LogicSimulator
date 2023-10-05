using FontStashSharp.RichText;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using MonoGame.Extended;
using MonoGame.Extended.Input;

using Myra.Graphics2D.UI;

using StoneRed.LogicSimulator.Simulation;
using StoneRed.LogicSimulator.Simulation.LogicGates.Interfaces;
using StoneRed.LogicSimulator.UserInterface.Windows;
using StoneRed.LogicSimulator.Utilities;
using StoneRed.LogicSimulator.WorldSaveSystem;

using System;
using System.Linq;

using IColorable = StoneRed.LogicSimulator.Simulation.LogicGates.Interfaces.IColorable;

namespace StoneRed.LogicSimulator.UserInterface.Screens;

internal class WorldScreen : SrlsScreen<Grid>
{
    private readonly LogicGateSimulator simulator;
    private readonly Vector2 logicGateSize = new Vector2(100, 100);
    private readonly WorldData worldData;
    private OrthographicCamera camera = null!;
    private RichTextLayout richTextLayout = null!;

    private Label fpsLabel = null!;
    private Label upsLabel = null!;
    private Label positionLabel = null!;
    private SpinButton frequency = null!;
    private CheckBox highPerformance = null!;
    private ListBox nativeComponentsListBox = null!;

    private float fps;

    private bool blockInput;

    private MouseStateExtended previousMouseState;

    private ConnectionContext? connectionContext = null;

    private LogicGate? selectedLogicGate = null;
    public override bool ScalingEnabled => false;
    protected override string XmmpPath => "WorldScreen.xmmp";

    public WorldScreen(WorldData worldData)
    {
        simulator = new LogicGateSimulator(worldData.LogicGates);
        this.worldData = worldData;
    }

    public WorldScreen()
    {
        simulator = new LogicGateSimulator(Enumerable.Empty<LogicGate>());
        worldData = new WorldData(Enumerable.Empty<LogicGate>(), Srls.CurrentSaveVersion, string.Empty);
    }

    protected override void Initialize()
    {
        VerticalStackPanel infoPanel = Root.FindChildById<VerticalStackPanel>("info");
        fpsLabel = infoPanel.FindChildById<Label>("fps");
        upsLabel = infoPanel.FindChildById<Label>("ups");
        positionLabel = infoPanel.FindChildById<Label>("position");

        VerticalStackPanel settingsPanel = Root.FindChildById<VerticalStackPanel>("settings");
        frequency = settingsPanel.FindChildById<SpinButton>("frequency");
        highPerformance = settingsPanel.FindChildById<CheckBox>("highPerformance");

        nativeComponentsListBox = Root.FindChildById<ListBox>("nativeComponents");

        foreach (LogicGateInfo logicGateInfo in srls.LogicGatesManager.GetNativeLogicGatesInfos())
        {
            nativeComponentsListBox.Items.Add(new ListItem(logicGateInfo.TypeName));
        }

        nativeComponentsListBox.SelectedIndexChanged += NativeComponentsListBox_SelectedIndexChanged;

        richTextLayout = new RichTextLayout
        {
            Font = srls.FontSystem.GetFont(15)
        };

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

        Matrix transformMatrix = camera.GetViewMatrix();
        srls.SpriteBatch.Begin(SpriteSortMode.BackToFront, samplerState: SamplerState.PointClamp, transformMatrix: transformMatrix);

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
                richTextLayout.Draw(srls.SpriteBatch, (new Vector2(0, 15) * srls.Scale) + (logicGate.WorldData.Position * srls.Scale), Color.Black, new Vector2(srls.Scale, srls.Scale), layerDepth: 0);
            }

            // Draw logic gate
            if (logicGate is Simulation.LogicGates.Interfaces.IDrawable drawable)
            {
                Rectangle rectangle = new Rectangle((logicGate.WorldData.Position * srls.Scale).ToPoint(), (logicGateSize * srls.Scale).ToPoint());
                srls.SpriteBatch.Draw(drawable.Texture, rectangle, null, color, 0, Vector2.Zero, SpriteEffects.None, 0.2f);
            }
            else
            {
                srls.SpriteBatch.FillRectangle(logicGate.WorldData.Position * srls.Scale, logicGateSize * srls.Scale, color, 0.2f);
            }

            // Draw text for components
            richTextLayout.Text = logicGate.WorldData.Name;
            richTextLayout.Draw(srls.SpriteBatch, logicGate.WorldData.Position * srls.Scale, Color.Black, new Vector2(srls.Scale, srls.Scale), layerDepth: 0);

            // Draw logic gate connections
            foreach (LogicGateConnection connection in logicGate.LogicGateConnections)
            {
                Color lineColor = logicGate.GetCachedOutputBit(connection.OutputIndex) == 1 ? Color.Red : Color.LightBlue;
                srls.SpriteBatch.DrawLine((logicGate.WorldData.Position * srls.Scale) + (logicGateSize / 2 * srls.Scale), (connection.LogicGate.WorldData.Position * srls.Scale) + (logicGateSize / 2 * srls.Scale), lineColor, 5 * srls.Scale, 0.1f);
            }
        }

        if (connectionContext is not null)
        {
            LogicGate logicGate = connectionContext.LogicGate;
            srls.SpriteBatch.DrawLine((logicGate.WorldData.Position * srls.Scale) + (logicGateSize / 2 * srls.Scale), camera.ScreenToWorld(previousMouseState.Position.ToVector2()), Color.Purple, 5 * srls.Scale, 0.1f);
        }

        if (selectedLogicGate is not null)
        {
            richTextLayout.Text = selectedLogicGate.WorldData.Name;
            richTextLayout.Draw(srls.SpriteBatch, selectedLogicGate.WorldData.Position, Color.White, new Vector2(srls.Scale, srls.Scale), layerDepth: 0);
            srls.SpriteBatch.DrawRectangle(selectedLogicGate.WorldData.Position, logicGateSize * srls.Scale, Color.Red, 2 * srls.Scale);
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
        upsLabel.Text = $"FRQ: {FrequencyCalculator.CalculateFrequency(simulator.ActualTicksPerSecond)}/{FrequencyCalculator.CalculateFrequency(simulator.TargetTicksPerSecond)}{(simulator.HighPerformanceClock ? "*" : string.Empty)} {(simulator.ClockCalibrating && simulator.HighPerformanceClock ? $"[Calibrating... {calibrationPercentage}%]" : string.Empty)}";
        positionLabel.Text = $"X/Y: {(long)Math.Round(camera.Position.X / logicGateSize.X / srls.Scale)}/{(long)Math.Round(camera.Position.Y / logicGateSize.Y / srls.Scale)}";

        simulator.TargetTicksPerSecond = (int)frequency.Value.GetValueOrDefault();
        simulator.HighPerformanceClock = highPerformance.IsChecked;

        fpsLabel.Font = srls.FontSystem.GetFont(10 * srls.Scale);
        upsLabel.Font = srls.FontSystem.GetFont(10 * srls.Scale);
        positionLabel.Font = srls.FontSystem.GetFont(10 * srls.Scale);

        if (blockInput)
        {
            blockInput = srls.WindowOpen;
            return;
        }

        MouseStateExtended mouseState = MouseExtended.GetState();
        KeyboardStateExtended keyboardState = KeyboardExtended.GetState();

        bool mouseOverGate = false;

        foreach (LogicGate logicGate in simulator.GetLogicGates())
        {
            Rectangle rectangle = new Rectangle((logicGate.WorldData.Position * srls.Scale).ToPoint(), (logicGateSize * srls.Scale).ToPoint());

            if (rectangle.Contains(camera.ScreenToWorld(mouseState.Position.ToVector2())))
            {
                if (mouseState.IsButtonDown(MouseButton.Right) && selectedLogicGate is null)
                {
                    ShowConnectionContextMenu(logicGate, mouseState.Position, keyboardState.IsShiftDown());
                }
                else if (!srls.Desktop.IsMouseOverGUI && keyboardState.IsKeyDown(Keys.X) && connectionContext is null)
                {
                    simulator.RemoveLogicGate(logicGate);
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

        if (selectedLogicGate is not null)
        {
            if (mouseState.IsButtonDown(MouseButton.Left) && !mouseOverGate && !srls.Desktop.IsMouseOverGUI)
            {
                selectedLogicGate.WorldData.Position /= srls.Scale;
                simulator.AddLogicGate(selectedLogicGate);

                if (keyboardState.IsShiftDown())
                {
                    SelectLogicGate();
                }
                else
                {
                    UnSelectLogicGate();
                }
            }
            else
            {
                Vector2 position = camera.ScreenToWorld(mouseState.Position.ToVector2());
                position = new Vector2(position.X - RealMod(position.X, 100 * srls.Scale), position.Y - RealMod(position.Y, 100 * srls.Scale));

                selectedLogicGate.WorldData.Position = position;
            }
        }

        if (keyboardState.IsKeyDown(Keys.C))
        {
            UnSelectLogicGate();
        }

        if (keyboardState.IsKeyDown(Keys.Q))
        {
            blockInput = true;

            UnSelectLogicGate();
            worldData.LogicGates = simulator.GetLogicGates();

            srls.ShowWindow(new QuickMenu(worldData));
        }

        if (keyboardState.IsKeyDown(Keys.Home))
        {
            camera.Position = Vector2.Zero;
        }

        previousMouseState = mouseState;

        float movementSpeed = (float)Math.Pow(200, 2 - camera.Zoom);

        movementSpeed = Math.Clamp(movementSpeed, 1000, 20000);

        camera.Move(keyboardState.GetMovementDirection() * movementSpeed * gameTime.GetElapsedSeconds());

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

    private static float RealMod(float x, float m)
    {
        float r = x % m;
        return r < 0 ? r + m : r;
    }

    private void SelectLogicGate()
    {
        if (nativeComponentsListBox.SelectedItem is not null)
        {
            connectionContext = null;
            selectedLogicGate = srls.LogicGatesManager.CreateLogicGate(nativeComponentsListBox.SelectedItem.Text);
            selectedLogicGate.WorldData.Name = nativeComponentsListBox.SelectedItem.Text;
        }
    }

    private void UnSelectLogicGate()
    {
        connectionContext = null;
        selectedLogicGate = null;
        nativeComponentsListBox.SelectedIndex = -1;
        nativeComponentsListBox.SelectedItem = null;
    }

    private void NativeComponentsListBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        SelectLogicGate();
    }

    private void ShowConnectionContextMenu(LogicGate logicGate, Point position, bool showInputs)
    {
        showInputs = logicGate.OutputCount <= 0 || (showInputs && logicGate.InputCount > 0);

        string buttonText = "Settings";

        if (connectionContext is not null)
        {
            showInputs = !connectionContext.IsInput;
            buttonText = "Cancel";
        }

        int count = showInputs ? logicGate.InputCount : logicGate.OutputCount;

        MenuItem[] menuItems = new MenuItem[count];

        for (int i = 0; i < count; i++)
        {
            int index = i;

            string connectionStatus = string.Empty;

            if (connectionContext is not null)
            {
                bool isConnected = false;

                if (connectionContext.IsInput)
                {
                    isConnected = logicGate.IsConnectedTo(connectionContext.LogicGate, connectionContext.Index, i);
                }
                else
                {
                    isConnected = connectionContext.LogicGate.IsConnectedTo(logicGate, i, connectionContext.Index);
                }

                connectionStatus = isConnected ? "[Disconnect]" : "[Connect]";
            }

            menuItems[i] = new MenuItem(i.ToString(), $"{connectionStatus} " + (showInputs ? $"Input {i}" : $"Output {i}"));
            menuItems[i].Selected += (_, _) => OnConnectionClicked(logicGate, index, showInputs);
        }

        TextButton button = srls.ShowContextMenu(logicGate.WorldData.Name, position, menuItems, true);
        button.Text = buttonText;
        button.Click += (_, _) =>
        {
            srls.Desktop.HideContextMenu();

            if (connectionContext is not null)
            {
                connectionContext = null;
            }
            else
            {
                // Show settings window
            }
        };
    }

    private void OnConnectionClicked(LogicGate logicGate, int index, bool isInput)
    {
        if (connectionContext is null)
        {
            connectionContext = new ConnectionContext(logicGate)
            {
                Index = index,
                IsInput = isInput
            };

            return;
        }

        if (connectionContext.IsInput)
        {
            if (logicGate.IsConnectedTo(connectionContext.LogicGate, connectionContext.Index, index))
            {
                logicGate.Disconnect(connectionContext.LogicGate, connectionContext.Index, index);
            }
            else
            {
                logicGate.Connect(connectionContext.LogicGate, connectionContext.Index, index);
            }
        }
        else
        {
            if (connectionContext.LogicGate.IsConnectedTo(logicGate, index, connectionContext.Index))
            {
                connectionContext.LogicGate.Disconnect(logicGate, index, connectionContext.Index);
            }
            else
            {
                connectionContext.LogicGate.Connect(logicGate, index, connectionContext.Index);
            }
        }

        connectionContext = null;
    }

    private sealed class ConnectionContext
    {
        public bool IsInput { get; set; }

        public LogicGate LogicGate { get; set; }

        public int Index { get; set; }

        public ConnectionContext(LogicGate logicGate)
        {
            LogicGate = logicGate;
        }
    }
}