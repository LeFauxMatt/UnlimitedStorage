using LeFauxMods.Common.Integrations.GenericModConfigMenu;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;

namespace LeFauxMods.UnlimitedStorage.Services;

internal sealed class UnlimitedOption : ComplexOption
{
    private readonly IModHelper helper;
    private readonly List<ParsedItemData> chests = [];
    private int height;

    public UnlimitedOption(IModHelper helper)
    {
        this.helper = helper;
        foreach (var itemId in ModState.ConfigHelper.Default.EnabledIds)
        {
            this.chests.Add(ItemRegistry.GetDataOrErrorItem($"(BC){itemId}"));
        }
    }

    /// <inheritdoc />
    public override int Height => this.height;

    /// <inheritdoc />
    public override string Name => I18n.ConfigOption_MakeUnlimited_Name();

    /// <inheritdoc />
    public override string Tooltip => I18n.ConfigOption_MakeUnlimited_Description();

    public override void Draw(SpriteBatch spriteBatch, Vector2 pos)
    {
        var availableWidth = Math.Min(1200, Game1.uiViewport.Width - 200);
        var (originX, originY) = pos.ToPoint();
        var (mouseX, mouseY) = this.helper.Input.GetCursorPosition().GetScaledScreenPixels().ToPoint();

        mouseX -= originX;
        mouseY -= originY;

        var mouseLeft = this.helper.Input.GetState(SButton.MouseLeft);
        var controllerA = this.helper.Input.GetState(SButton.ControllerA);
        var pressed = mouseLeft is SButtonState.Pressed || controllerA is SButtonState.Pressed;
        var hoverText = default(string);

        foreach (var chest in this.chests)
        {
            var sourceRect = chest.GetSourceRect();
            var bounds = new Rectangle(
                (int)pos.X,
                (int)pos.Y,
                sourceRect.Width * Game1.pixelZoom,
                sourceRect.Height * Game1.pixelZoom);

            spriteBatch.Draw(
                chest.GetTexture(),
                bounds,
                sourceRect,
                ModState.ConfigHelper.Temp.EnabledIds.Contains(chest.ItemId) ? Color.White * 1f : Color.Gray * 0.8f,
                0f,
                Vector2.Zero,
                SpriteEffects.None,
                1f);

            if ((bounds with { X = bounds.X - originX, Y = bounds.Y - originY }).Contains(mouseX, mouseY))
            {
                hoverText ??= chest.DisplayName;
                if (pressed && !ModState.ConfigHelper.Temp.EnabledIds.Add(chest.ItemId))
                {
                    ModState.ConfigHelper.Temp.EnabledIds.Remove(chest.ItemId);
                }
            }

            pos.X += Game1.tileSize + 16;
            if (pos.X + Game1.tileSize + 16 > availableWidth)
            {
                pos.X = originX;
                pos.Y += (Game1.tileSize * 2) + 16;
            }
        }

        pos.Y += Game1.tileSize * 2;
        this.height = (int)(pos.Y - originY);

        if (!string.IsNullOrWhiteSpace(hoverText))
        {
            IClickableMenu.drawToolTip(spriteBatch, hoverText, null, null);
        }
    }
}