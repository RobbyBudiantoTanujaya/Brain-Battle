using UnityEngine;

namespace BrainBattle.Shared.UI
{
    public static class DesignSystem
    {
        // Colors
        public static readonly Color Primary       = new Color(1f, 0.176f, 0.471f, 1f);        // #ff2d78
        public static readonly Color PrimaryDark   = new Color(0.769f, 0f, 0.435f, 1f);        // #c4006f
        public static readonly Color Background    = new Color(0.102f, 0.102f, 0.118f, 1f);    // #1a1a1e
        public static readonly Color Surface       = new Color(0.165f, 0.165f, 0.243f, 1f);    // #2a2a3e
        public static readonly Color TextPrimary   = Color.white;
        public static readonly Color TextSecondary = new Color(0.533f, 0.533f, 0.533f, 1f);   // #888888
        public static readonly Color Overlay       = new Color(0f, 0f, 0f, 0.7f);
        public static readonly Color BorderRegion  = new Color(0f, 0f, 0f, 0.8f);
        public static readonly Color BorderCell    = new Color(0f, 0f, 0f, 0.2f);

        // HUD Bar
        public static readonly Color HUDBarGradientTop    = new Color(0.075f, 0.075f, 0.165f, 1f);   // #13132a
        public static readonly Color HUDBarGradientBottom = new Color(0.051f, 0.051f, 0.102f, 1f);   // #0d0d1a
        public static readonly Color HUDBorderSeparator   = new Color(1f, 1f, 1f, 0.08f);            // rgba(255,255,255,0.08)
        public static readonly Color HUDButtonBg          = new Color(1f, 1f, 1f, 0.05f);            // rgba(255,255,255,0.05)
        public static readonly Color HUDButtonBorder      = new Color(1f, 1f, 1f, 0.08f);            // rgba(255,255,255,0.08)
        public static readonly Color HUDMoveCounterBg     = new Color(1f, 0.176f, 0.471f, 0.12f);    // rgba(255,45,120,0.12)
        public static readonly Color HUDMoveCounterBorder = new Color(1f, 0.176f, 0.471f, 0.30f);    // rgba(255,45,120,0.30)
        public static readonly Color HUDTipsBg            = new Color(1f, 0.176f, 0.471f, 0.15f);    // rgba(255,45,120,0.15)
        public static readonly Color HUDTipsBorder        = new Color(1f, 0.176f, 0.471f, 0.35f);    // rgba(255,45,120,0.35)
        public static readonly Color HUDLabelText         = new Color(0.533f, 0.533f, 0.667f, 1f);   // #8888aa
        public static readonly Color HUDMoveCounterLabel  = new Color(1f, 0.176f, 0.471f, 0.55f);    // rgba(255,45,120,0.55)

        // Typography
        public static readonly int FontSizeSmall  = 12;
        public static readonly int FontSizeBody   = 16;
        public static readonly int FontSizeMedium = 18;
        public static readonly int FontSizeLarge  = 20;
        public static readonly int FontSizeTitle  = 28;
        public static readonly int FontSizeHero   = 36;

        // Spacing
        public static readonly float SpacingXS  = 4f;
        public static readonly float SpacingS   = 8f;
        public static readonly float SpacingM   = 12f;
        public static readonly float SpacingL   = 16f;
        public static readonly float SpacingXL  = 24f;
        public static readonly float SpacingXXL = 32f;

        // Border Radius
        public static readonly float RadiusS = 8f;
        public static readonly float RadiusM = 12f;
        public static readonly float RadiusL = 16f;

        // Component sizes
        public static readonly float HUDHeight         = 80f;
        public static readonly float TimerBarHeight    = 48f;
        public static readonly float ButtonHeight      = 64f;
        public static readonly float TabHeight         = 60f;
        public static readonly float ProgressBarHeight = 6f;
        public static readonly float LevelButtonSize   = 100f;
        public static readonly float LevelButtonGap    = 12f;

        // Grid
        public static readonly float GridPadding           = 16f;
        public static readonly float BorderRegionThickness = 5f;   // thick border between different regions
        public static readonly float BorderCellThickness   = 3f;   // thin border between same-region cells
        public static readonly float CrownSizeRatio        = 0.65f;
        public static readonly float DotSizeRatio          = 0.25f;
    }
}
