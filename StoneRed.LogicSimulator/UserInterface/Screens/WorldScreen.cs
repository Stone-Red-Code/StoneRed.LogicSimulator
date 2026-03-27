using FontStashSharp.RichText;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using MonoGame.Extended;
using MonoGame.Extended.Input;

using Myra.Graphics2D.UI;

using StoneRed.LogicSimulator.Api;
using StoneRed.LogicSimulator.Api.Interfaces;
using StoneRed.LogicSimulator.Simulation;
using StoneRed.LogicSimulator.UserInterface.Windows;
using StoneRed.LogicSimulator.Utilities;
using StoneRed.LogicSimulator.WorldSaveSystem;

using System;
using System.Collections.Generic;
using System.Linq;

using IColorable = StoneRed.LogicSimulator.Api.Interfaces.IColorable;

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
    private SpinButton frequencySpinButton = null!;
    private CheckBox highPerformanceCheckBox = null!;
    private ComboBox simulatorTypeComboBox = null!;
    private CheckBox hideCablesBehindGatesCheckBox = null!;
    private ListBox nativeComponentsListBox = null!;

    private float fps;

    private bool blockInput;

    private MouseStateExtended previousMouseState;

    private ConnectionContext? connectionContext = null;

    private LogicGate? selectedLogicGate = null;
    private bool selectedLogicGateIsExisting;
    private readonly HashSet<LogicGate> selectedLogicGates = [];
    private bool isBoxSelecting;
    private bool isMovingSelection;
    private Vector2 selectionStartWorld;
    private Vector2 selectionEndWorld;
    private readonly Dictionary<LogicGate, Vector2> moveOffsets = [];
    private readonly List<ClipboardGateData> clipboardGates = [];
    private readonly List<ClipboardConnectionData> clipboardConnections = [];
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
        frequencySpinButton = settingsPanel.FindChildById<SpinButton>("frequency");
        highPerformanceCheckBox = settingsPanel.FindChildById<CheckBox>("highPerformance");
        simulatorTypeComboBox = settingsPanel.FindChildById<ComboBox>("simulatorType");
        simulatorTypeComboBox.SelectedIndex = 0; // Default to Cycle
        simulatorTypeComboBox.SelectedIndexChanged += SimulatorTypeComboBox_SelectedIndexChanged;
        hideCablesBehindGatesCheckBox = settingsPanel.FindChildById<CheckBox>("hideCablesBehindGates");

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
                richTextLayout.Draw(srls.SpriteBatch, (new Vector2(2, 17) + logicGate.WorldData.Position) * srls.Scale, Color.Black, new Vector2(srls.Scale, srls.Scale), layerDepth: 0);
            }

            // Draw logic gate
            if (logicGate is Api.Interfaces.IDrawable drawable)
            {
                Rectangle rectangle = new Rectangle((logicGate.WorldData.Position * srls.Scale).ToPoint(), (logicGateSize * srls.Scale).ToPoint());
                srls.SpriteBatch.Draw(drawable.Texture, rectangle, null, color, 0, Vector2.Zero, SpriteEffects.None, 0.3f);
            }
            else
            {
                srls.SpriteBatch.FillRectangle(logicGate.WorldData.Position * srls.Scale, logicGateSize * srls.Scale, color, 0.3f);
            }

            srls.SpriteBatch.DrawRectangle(logicGate.WorldData.Position * srls.Scale, logicGateSize * srls.Scale, Color.DarkGray, 2 * srls.Scale, 0.2f);

            if (selectedLogicGates.Contains(logicGate))
            {
                srls.SpriteBatch.DrawRectangle(logicGate.WorldData.Position * srls.Scale, logicGateSize * srls.Scale, Color.Gold, 3 * srls.Scale, 0.15f);
            }

            // Draw text for components
            richTextLayout.Text = logicGate.WorldData.Name.Truncate(10);
            richTextLayout.Draw(srls.SpriteBatch, (logicGate.WorldData.Position + new Vector2(2, 2)) * srls.Scale, Color.Black, new Vector2(srls.Scale, srls.Scale), layerDepth: 0);

            // Draw logic gate connections
            foreach (LogicGateConnection connection in logicGate.LogicGateConnections)
            {
                Color lineColor = simulator.GetGateOutput(logicGate) ? Color.Red : Color.LightBlue;
                srls.SpriteBatch.DrawLine((logicGate.WorldData.Position * srls.Scale) + (logicGateSize / 2 * srls.Scale), (connection.LogicGate.WorldData.Position * srls.Scale) + (logicGateSize / 2 * srls.Scale), lineColor, 5 * srls.Scale, hideCablesBehindGatesCheckBox.IsChecked ? 0.5f : 0.1f);
            }
        }

        if (connectionContext is not null)
        {
            LogicGate logicGate = connectionContext.LogicGate;
            srls.SpriteBatch.DrawLine((logicGate.WorldData.Position * srls.Scale) + (logicGateSize / 2 * srls.Scale), camera.ScreenToWorld(previousMouseState.Position.ToVector2()), Color.Purple, 5 * srls.Scale, 0.1f);
        }

        if (selectedLogicGate is not null)
        {
            richTextLayout.Text = selectedLogicGate.WorldData.Name.Truncate(10);
            richTextLayout.Draw(srls.SpriteBatch, (selectedLogicGate.WorldData.Position + new Vector2(2, 2)) * srls.Scale, Color.White, new Vector2(srls.Scale, srls.Scale), layerDepth: 0);
            srls.SpriteBatch.DrawRectangle(selectedLogicGate.WorldData.Position * srls.Scale, logicGateSize * srls.Scale, Color.Red, 2 * srls.Scale);
        }

        if (isBoxSelecting)
        {
            Rectangle rect = CreateSelectionRectangle(selectionStartWorld, selectionEndWorld);
            srls.SpriteBatch.DrawRectangle(rect, Color.LimeGreen, 2 * srls.Scale, 0.12f);
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

        simulator.TargetTicksPerSecond = (int)frequencySpinButton.Value.GetValueOrDefault();
        simulator.HighPerformanceClock = highPerformanceCheckBox.IsChecked;

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
        Vector2 mouseWorld = camera.ScreenToWorld(mouseState.Position.ToVector2());

        bool mouseOverGate = false;

        foreach (LogicGate logicGate in simulator.GetLogicGates())
        {
            if (logicGate == selectedLogicGate)
            {
                continue;
            }

            Rectangle rectangle = new Rectangle((logicGate.WorldData.Position * srls.Scale).ToPoint(), (logicGateSize * srls.Scale).ToPoint());

            if (rectangle.Contains(camera.ScreenToWorld(mouseState.Position.ToVector2())))
            {
                if (mouseState.IsButtonDown(MouseButton.Right) && selectedLogicGate is null)
                {
                    ShowConnectionContextMenu(logicGate, mouseState.Position, keyboardState.IsShiftDown());
                }
                else if (!srls.Desktop.IsMouseOverGUI && (keyboardState.IsKeyDown(Keys.X) || keyboardState.IsKeyDown(Keys.Delete)) && connectionContext is null)
                {
                    simulator.RemoveLogicGate(logicGate);
                }
                else if (!srls.Desktop.IsMouseOverGUI && keyboardState.IsKeyDown(Keys.M) && selectedLogicGates.Count == 0 && connectionContext is null && selectedLogicGate is null)
                {
                    selectedLogicGate = logicGate;
                    selectedLogicGateIsExisting = true;
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

        if (!srls.Desktop.IsMouseOverGUI && connectionContext is null && selectedLogicGate is null)
        {
            if (mouseState.WasButtonJustDown(MouseButton.Left) && !mouseOverGate && !isMovingSelection)
            {
                isBoxSelecting = true;
                selectionStartWorld = mouseWorld;
                selectionEndWorld = mouseWorld;
                selectedLogicGates.Clear();
            }
            else if (isBoxSelecting && mouseState.IsButtonDown(MouseButton.Left))
            {
                selectionEndWorld = mouseWorld;
            }
            else if (isBoxSelecting && mouseState.WasButtonJustUp(MouseButton.Left))
            {
                isBoxSelecting = false;
                selectionEndWorld = mouseWorld;
                selectedLogicGates.Clear();
                Rectangle selectionRect = CreateSelectionRectangle(selectionStartWorld, selectionEndWorld);
                foreach (LogicGate logicGate in simulator.GetLogicGates())
                {
                    Rectangle gateRect = new Rectangle((logicGate.WorldData.Position * srls.Scale).ToPoint(), (logicGateSize * srls.Scale).ToPoint());
                    if (selectionRect.Intersects(gateRect))
                    {
                        _ = selectedLogicGates.Add(logicGate);
                    }
                }
            }
        }

        if (!srls.Desktop.IsMouseOverGUI && selectedLogicGates.Count > 0 && connectionContext is null && selectedLogicGate is null)
        {
            if (!isMovingSelection && keyboardState.WasKeyJustUp(Keys.M))
            {
                isMovingSelection = true;
                moveOffsets.Clear();
                Vector2 anchor = SnapToGrid(mouseWorld / srls.Scale);
                foreach (LogicGate logicGate in selectedLogicGates)
                {
                    moveOffsets[logicGate] = logicGate.WorldData.Position - anchor;
                }
            }

            if (keyboardState.WasKeyJustUp(Keys.X) || keyboardState.WasKeyJustUp(Keys.Delete))
            {
                foreach (LogicGate logicGate in selectedLogicGates.ToList())
                {
                    simulator.RemoveLogicGate(logicGate);
                }
                selectedLogicGates.Clear();
                isMovingSelection = false;
                moveOffsets.Clear();
            }
        }

        if (!srls.Desktop.IsMouseOverGUI && connectionContext is null && selectedLogicGate is null && keyboardState.IsControlDown())
        {
            if (keyboardState.WasKeyJustUp(Keys.C))
            {
                CopySelectionToClipboard();
            }
            else if (keyboardState.WasKeyJustUp(Keys.V))
            {
                PasteClipboardAt(SnapToGrid(mouseWorld / srls.Scale));
                isMovingSelection = true;
                moveOffsets.Clear();
                Vector2 anchor = SnapToGrid(mouseWorld / srls.Scale);
                foreach (LogicGate logicGate in selectedLogicGates)
                {
                    moveOffsets[logicGate] = logicGate.WorldData.Position - anchor;
                }
            }
        }

        if (isMovingSelection)
        {
            Vector2 anchor = SnapToGrid(mouseWorld / srls.Scale);
            foreach (LogicGate logicGate in selectedLogicGates)
            {
                if (moveOffsets.TryGetValue(logicGate, out Vector2 offset))
                {
                    logicGate.WorldData.Position = anchor + offset;
                }
            }

            if (mouseState.WasButtonJustDown(MouseButton.Left))
            {
                isMovingSelection = false;
                moveOffsets.Clear();
            }
        }

        if (selectedLogicGate is not null)
        {
            // Convert screen to world coordinates and then to unscaled grid coordinates
            Vector2 position = camera.ScreenToWorld(mouseState.Position.ToVector2()) / srls.Scale;

            // Snap to 100-unit grid (in unscaled coordinates)
            position = new Vector2(position.X - RealMod(position.X, 100), position.Y - RealMod(position.Y, 100));

            selectedLogicGate.WorldData.Position = position;

            if (mouseState.WasButtonJustDown(MouseButton.Left) && !mouseOverGate && !srls.Desktop.IsMouseOverGUI)
            {
                if (!selectedLogicGateIsExisting)
                {
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
                    simulator.LogicGatesUpdated();
                    UnSelectLogicGate();
                }
            }
        }

        if (!keyboardState.IsControlDown() && keyboardState.WasKeyJustUp(Keys.C))
        {
            UnSelectLogicGate();
        }

        if (keyboardState.WasKeyJustUp(Keys.Escape))
        {
            blockInput = true;

            UnSelectLogicGate();
            worldData.LogicGates = simulator.GetLogicGates();

            srls.ShowWindow(new QuickMenu(worldData));
        }

        if (keyboardState.WasKeyJustUp(Keys.Home))
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

    private static Vector2 SnapToGrid(Vector2 position)
    {
        return new Vector2(
            position.X - RealMod(position.X, 100),
            position.Y - RealMod(position.Y, 100)
        );
    }

    private static Rectangle CreateSelectionRectangle(Vector2 start, Vector2 end)
    {
        int left = (int)Math.Min(start.X, end.X);
        int top = (int)Math.Min(start.Y, end.Y);
        int right = (int)Math.Max(start.X, end.X);
        int bottom = (int)Math.Max(start.Y, end.Y);
        return new Rectangle(left, top, Math.Max(1, right - left), Math.Max(1, bottom - top));
    }

    private void CopySelectionToClipboard()
    {
        clipboardGates.Clear();
        clipboardConnections.Clear();

        if (selectedLogicGates.Count == 0)
        {
            return;
        }

        LogicGate[] gates = selectedLogicGates.ToArray();
        Vector2 min = new Vector2(gates.Min(g => g.WorldData.Position.X), gates.Min(g => g.WorldData.Position.Y));

        Dictionary<LogicGate, int> gateIndexes = [];
        for (int i = 0; i < gates.Length; i++)
        {
            gateIndexes[gates[i]] = i;
        }

        for (int i = 0; i < gates.Length; i++)
        {
            LogicGate source = gates[i];
            if (!LogicGatesManager.TryGetTypeLogicGateName(source.GetType(), out string? typeName))
            {
                continue;
            }

            clipboardGates.Add(new ClipboardGateData(
                typeName,
                source.WorldData.Position - min,
                source.WorldData.Name,
                source.WorldData.Description
            ));
        }

        foreach (LogicGate source in gates)
        {
            if (!gateIndexes.TryGetValue(source, out int fromIndex))
            {
                continue;
            }

            foreach (LogicGateConnection connection in source.LogicGateConnections)
            {
                if (gateIndexes.TryGetValue(connection.LogicGate, out int toIndex))
                {
                    clipboardConnections.Add(new ClipboardConnectionData(
                        fromIndex,
                        toIndex,
                        connection.InputIndex,
                        connection.OutputIndex
                    ));
                }
            }
        }
    }

    private void PasteClipboardAt(Vector2 anchor)
    {
        if (clipboardGates.Count == 0)
        {
            return;
        }

#pragma warning disable IDE0028 // Simplify collection initialization
        List<LogicGate> pastedGates = new List<LogicGate>(clipboardGates.Count);
#pragma warning restore IDE0028 // Simplify collection initialization

        for (int i = 0; i < clipboardGates.Count; i++)
        {
            ClipboardGateData gateData = clipboardGates[i];
            LogicGate gate = srls.LogicGatesManager.CreateLogicGate(gateData.TypeName);
            gate.WorldData.Name = gateData.Name;
            gate.WorldData.Description = gateData.Description;
            gate.WorldData.Position = anchor + gateData.RelativePosition;
            simulator.AddLogicGate(gate);
            pastedGates.Add(gate);
        }

        foreach (ClipboardConnectionData connection in clipboardConnections)
        {
            if (connection.FromIndex < 0 || connection.FromIndex >= pastedGates.Count ||
                connection.ToIndex < 0 || connection.ToIndex >= pastedGates.Count)
            {
                continue;
            }

            LogicGate fromGate = pastedGates[connection.FromIndex];
            LogicGate toGate = pastedGates[connection.ToIndex];
            fromGate.Connect(toGate, connection.InputIndex, connection.OutputIndex);
        }

        simulator.LogicGatesUpdated();
        selectedLogicGates.Clear();
        foreach (LogicGate gate in pastedGates)
        {
            _ = selectedLogicGates.Add(gate);
        }
        isMovingSelection = false;
        moveOffsets.Clear();
    }

    private void SelectLogicGate()
    {
        if (nativeComponentsListBox.SelectedItem is not null)
        {
            connectionContext = null;
            selectedLogicGate = srls.LogicGatesManager.CreateLogicGate(nativeComponentsListBox.SelectedItem.Text);
            selectedLogicGateIsExisting = false;
            selectedLogicGate.WorldData.Name = nativeComponentsListBox.SelectedItem.Text;
        }
    }

    private void UnSelectLogicGate()
    {
        connectionContext = null;
        selectedLogicGate = null;
        selectedLogicGateIsExisting = false;
        selectedLogicGates.Clear();
        isBoxSelecting = false;
        isMovingSelection = false;
        moveOffsets.Clear();
        nativeComponentsListBox.SelectedIndex = -1;
        nativeComponentsListBox.SelectedItem = null;
    }

    private void NativeComponentsListBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        SelectLogicGate();
    }

    private void SimulatorTypeComboBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        simulator.SimulatorType = simulatorTypeComboBox.SelectedIndex switch
        {
            1 => SimulatorType.Event,
            _ => SimulatorType.Cycle
        };
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

        simulator.LogicGatesUpdated();
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

    private sealed record ClipboardGateData(string TypeName, Vector2 RelativePosition, string Name, string Description);

    private sealed record ClipboardConnectionData(int FromIndex, int ToIndex, int InputIndex, int OutputIndex);
}
