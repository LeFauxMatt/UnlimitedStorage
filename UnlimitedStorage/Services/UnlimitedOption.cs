using System.Collections.Immutable;
using System.Globalization;
using LeFauxMods.Common.Integrations.GenericModConfigMenu;
using LeFauxMods.Common.Models;
using LeFauxMods.UnlimitedStorage.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.BellsAndWhistles;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using StardewValley.TokenizableStrings;

namespace LeFauxMods.UnlimitedStorage.Services;

internal sealed class UnlimitedOption : ComplexOption
{
    private readonly int baseHeight;
    private readonly Dictionary<string, CachedItemData> cachedItems = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<ClickableComponent> components = [];
    private readonly int height;
    private int selectedIndex = -1;

    public UnlimitedOption(IModHelper helper)
        : base(helper)
    {
        var itemIds = ModState.ConfigHelper.Default.StorageOptions.Keys.ToImmutableArray();
        this.baseHeight = ((Game1.tileSize * 2) + 8) * (int)Math.Ceiling(itemIds.Length / 14f);

        ClickableComponent component;
        for (var index = 0; index < itemIds.Length; index++)
        {
            var itemId = itemIds[index];
            var row = index / 14;
            var col = index % 14;

            component = new ClickableComponent(
                new Rectangle(
                    col * (Game1.tileSize + 8),
                    row * ((Game1.tileSize * 2) + 8),
                    Game1.tileSize,
                    Game1.tileSize * 2),
                itemId) { myID = index };

            var parsedItemData = ItemRegistry.GetDataOrErrorItem($"(BC){itemId}");
            var sourceRect = parsedItemData.GetSourceRect(0, parsedItemData.SpriteIndex);

            this.components.Add(component);
            this.cachedItems.Add(itemId, new CachedItemData(parsedItemData, sourceRect));
        }

        this.height = this.baseHeight + 16;

        var (textWidth, textHeight) = Game1.dialogueFont.MeasureString(I18n.ConfigOption_Enabled_Name()).ToPoint();
        component = new ClickableComponent(
            new Rectangle(0, this.height, textWidth, textHeight),
            "config-option.enabled.description",
            I18n.ConfigOption_Enabled_Name());
        this.components.Add(component);

        component = new ClickableTextureComponent(
            "enabled",
            new Rectangle(
                Math.Min(1200, Game1.uiViewport.Width - 200) / 2,
                this.height,
                OptionsCheckbox.sourceRectChecked.Width * Game1.pixelZoom,
                OptionsCheckbox.sourceRectChecked.Height * Game1.pixelZoom),
            null,
            null,
            Game1.mouseCursors,
            OptionsCheckbox.sourceRectChecked,
            Game1.pixelZoom);

        this.components.Add(component);
        this.height += textHeight + 16;

        (textWidth, textHeight) = Game1.dialogueFont.MeasureString(I18n.ConfigOption_Unlimited_Name()).ToPoint();
        component = new ClickableComponent(
            new Rectangle(0, this.height, textWidth, textHeight),
            "config-option.unlimited.description",
            I18n.ConfigOption_Unlimited_Name());
        this.components.Add(component);

        component = new ClickableTextureComponent(
            "unlimited",
            new Rectangle(
                Math.Min(1200, Game1.uiViewport.Width - 200) / 2,
                this.height,
                OptionsCheckbox.sourceRectChecked.Width * Game1.pixelZoom,
                OptionsCheckbox.sourceRectChecked.Height * Game1.pixelZoom),
            null,
            null,
            Game1.mouseCursors,
            OptionsCheckbox.sourceRectChecked,
            Game1.pixelZoom);

        this.components.Add(component);
        this.height += textHeight + 16;

        (textWidth, textHeight) = Game1.dialogueFont.MeasureString(I18n.ConfigOption_MenuSize_Name()).ToPoint();
        component = new ClickableComponent(
            new Rectangle(0, this.height, textWidth, textHeight),
            "config-option.menu-size.description",
            I18n.ConfigOption_MenuSize_Name());

        this.components.Add(component);

        this.height += textHeight;

        for (var index = 0; index < 70; index++)
        {
            var row = index / 14;
            var col = index % 14;

            component = new ClickableComponent(
                new Rectangle(
                    col * Game1.tileSize,
                    (row * Game1.tileSize) + this.height + 16,
                    Game1.tileSize,
                    Game1.tileSize),
                index.ToString(CultureInfo.InvariantCulture)) { myID = index };

            this.components.Add(component);
        }

        this.height += Game1.tileSize * 5;
    }

    /// <inheritdoc />
    public override int Height => this.selectedIndex != -1 ? this.height : this.baseHeight;

    public override void DrawOption(SpriteBatch spriteBatch, Vector2 pos)
    {
        var (mouseX, mouseY) = this.MousePos;
        var hoverTitle = default(string);
        var hoverText = default(string);
        StorageOptions? storageOptions = null;

        if (this.selectedIndex != -1 &&
            this.cachedItems.TryGetValue(this.components[this.selectedIndex].name, out var cachedItem) &&
            !ModState.ConfigHelper.Temp.StorageOptions.TryGetValue(cachedItem.Data.ItemId, out storageOptions))
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            storageOptions = new StorageOptions(new DictionaryModel(() => values));
            ModState.ConfigHelper.Temp.StorageOptions.Add(cachedItem.Data.ItemId, storageOptions);
        }

        foreach (var component in this.components)
        {
            var hovered = component.bounds.Contains(mouseX, mouseY);

            if (this.cachedItems.TryGetValue(component.name, out cachedItem))
            {
                var (parsedItemData, sourceRect) = cachedItem;

                component.scale = Math.Max(1f, component.scale - 0.025f);
                if (hovered)
                {
                    component.scale = Math.Min(component.scale + 0.05f, 1.1f);
                    hoverText ??= TokenParser.ParseText(parsedItemData.DisplayName);
                    if (this.Pressed)
                    {
                        Game1.playSound("smallSelect");
                        this.selectedIndex = component.myID;
                    }
                }

                spriteBatch.Draw(
                    parsedItemData.GetTexture(),
                    new Rectangle(
                        (int)pos.X + component.bounds.Center.X,
                        (int)pos.Y + component.bounds.Center.Y,
                        sourceRect.Width * Game1.pixelZoom,
                        sourceRect.Height * Game1.pixelZoom),
                    sourceRect,
                    component.myID == this.selectedIndex ? Color.White * 1f : Color.Gray * 0.8f,
                    0f,
                    sourceRect.Size.ToVector2() / 2f,
                    SpriteEffects.None,
                    1f);

                continue;
            }

            if (storageOptions is null)
            {
                continue;
            }

            if (component is ClickableTextureComponent clickableTextureComponent)
            {
                if (hovered && this.Pressed)
                {
                    Game1.playSound("drumkit6");
                    switch (component.name)
                    {
                        case "enabled":
                            storageOptions.Enabled = !storageOptions.Enabled;
                            break;
                        case "unlimited":
                            storageOptions.Unlimited = !storageOptions.Unlimited;
                            break;
                    }
                }

                var isChecked = component.name switch
                {
                    "enabled" => storageOptions.Enabled,
                    "unlimited" => storageOptions.Unlimited,
                    _ => false
                };

                clickableTextureComponent.sourceRect = isChecked
                    ? OptionsCheckbox.sourceRectChecked
                    : OptionsCheckbox.sourceRectUnchecked;

                clickableTextureComponent.draw(
                    spriteBatch,
                    Color.White,
                    1f,
                    0,
                    (int)pos.X,
                    (int)pos.Y);

                continue;
            }

            if (int.TryParse(component.name, out var index))
            {
                var row = index / 14;
                var col = index % 14;

                spriteBatch.Draw(
                    Game1.menuTexture,
                    pos + component.bounds.Location.ToVector2(),
                    Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 10),
                    Color.White,
                    0f,
                    Vector2.Zero,
                    1f,
                    SpriteEffects.None,
                    0.5f);

                if (row >= storageOptions.MenuHeight || col >= storageOptions.MenuWidth)
                {
                    spriteBatch.Draw(
                        Game1.menuTexture,
                        pos + component.bounds.Location.ToVector2(),
                        Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 57),
                        Color.LightGray * 0.5f,
                        0f,
                        Vector2.Zero,
                        1f,
                        SpriteEffects.None,
                        0.5f);
                }

                if (hovered)
                {
                    hoverText ??= $"{col + 1} x {row + 1}";
                    spriteBatch.Draw(
                        Game1.menuTexture,
                        pos + component.bounds.Location.ToVector2(),
                        Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 56),
                        Color.Red,
                        0f,
                        Vector2.Zero,
                        1f,
                        SpriteEffects.None,
                        0.5f);

                    if (this.Pressed)
                    {
                        storageOptions.MenuWidth = col + 1;
                        storageOptions.MenuHeight = row + 1;
                    }
                }

                continue;
            }

            if (component.bounds.Contains(mouseX, mouseY))
            {
                hoverTitle ??= component.label;
                hoverText ??= this.Helper.Translation.Get(component.name);
            }

            Utility.drawTextWithShadow(
                spriteBatch,
                component.label,
                Game1.dialogueFont,
                pos + component.bounds.Location.ToVector2(),
                SpriteText.color_Gray);
        }

        if (!string.IsNullOrWhiteSpace(hoverTitle))
        {
            IClickableMenu.drawHoverText(spriteBatch, hoverText, Game1.smallFont, boldTitleText: hoverTitle);
        }
        else if (!string.IsNullOrWhiteSpace(hoverText))
        {
            IClickableMenu.drawHoverText(spriteBatch, hoverText, Game1.smallFont);
        }
    }

    private readonly record struct CachedItemData(ParsedItemData Data, Rectangle SourceRect);
}