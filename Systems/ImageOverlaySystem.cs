using Game.Audio;
using Game.Prefabs;
using Game.Simulation;
using Game;
using System;
using System.IO;
using System.Reflection;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;
using MapImageLayer.Helpers;

namespace MapImageLayer.Systems
{
    /// <summary>
    /// Overlays an image on world using a custom shader loaded via AssetBundle
    /// </summary>
    /// <remarks>
    /// Currently uses a flat plane
    /// </remarks>
    public class ImageOverlaySystem : GameSystemBase
    {
        public bool ShowImageOverlay
        {
            get;
            private set;
        } = false;

        public Texture2D ImageOverlay
        {
            get
            {
                return overlayTexture;
            }
        }

        private Texture2D overlayTexture;
        private GameObject imageOverlayObj;
        private DateTime lastOverlayCheck;
        private Renderer renderer;
        private float transparency = 0.6f;
        private ToolUXSoundSettingsData soundSettings;
        private string inputFile;
        private MapImageLayerConfig _config;

        protected override void OnCreate( )
        {
            base.OnCreate( );

            _config = MapImageLayerConfig.Load( );

            if ( !string.IsNullOrEmpty( _config.Image ) )
                inputFile = _config.Image;

            CreateKeyBinding( );

            Debug.Log( "ImagerOverlaySystem OnCreate" );
        }

        protected override void OnUpdate( )
        {
        }

        private void CreateKeyBinding( )
        {
            var inputAction = new InputAction( "ToggleImageOverlay" );
            inputAction.AddCompositeBinding( "ButtonWithOneModifier" )
                .With( "Modifier", "<Keyboard>/ctrl" )
                .With( "Button", "<Keyboard>/i" );
            inputAction.performed += OnToggleImageOverlay;
            inputAction.Enable( );

            inputAction = new InputAction( "ImageOverlayHeightIncrease" );
            inputAction.AddCompositeBinding( "ButtonWithOneModifier" )
                .With( "Modifier", "<Keyboard>/ctrl" )
                .With( "Button", "<Keyboard>/equals" );
            inputAction.performed += OnImageOverlayIncreaseHeight;
            inputAction.Enable( );

            inputAction = new InputAction( "ImageOverlayHeightDecrease" );
            inputAction.AddCompositeBinding( "ButtonWithOneModifier" )
                .With( "Modifier", "<Keyboard>/ctrl" )
                .With( "Button", "<Keyboard>/minus" );
            inputAction.performed += OnImageOverlayDecreaseHeight;
            inputAction.Enable( );

            inputAction = new InputAction( "ImageOverlaySizeIncrease" );
            inputAction.AddCompositeBinding( "ButtonWithOneModifier" )
                .With( "Modifier", "<Keyboard>/shift" )
                .With( "Button", "<Keyboard>/equals" );
            inputAction.performed += OnImageOverlayIncreaseSize;
            inputAction.Enable( );

            inputAction = new InputAction( "ImageOverlaySizeDecrease" );
            inputAction.AddCompositeBinding( "ButtonWithOneModifier" )
                .With( "Modifier", "<Keyboard>/shift" )
                .With( "Button", "<Keyboard>/minus" );
            inputAction.performed += OnImageOverlayDecreaseSize;
            inputAction.Enable( );

            inputAction = new InputAction( "ImageOverlayTransparencyIncrease" );
            inputAction.AddCompositeBinding( "ButtonWithOneModifier" )
                .With( "Modifier", "<Keyboard>/alt" )
                .With( "Button", "<Keyboard>/equals" );
            inputAction.performed += OnImageOverlayIncreaseTransparency;
            inputAction.Enable( );

            inputAction = new InputAction( "ImageOverlayTransparencyDecrease" );
            inputAction.AddCompositeBinding( "ButtonWithOneModifier" )
                .With( "Modifier", "<Keyboard>/alt" )
                .With( "Button", "<Keyboard>/minus" );
            inputAction.performed += OnImageOverlayDecreaseTransparency;
            inputAction.Enable( );

            inputAction = new InputAction( "ImageOverlayChoose" );
            inputAction.AddCompositeBinding( "ButtonWithOneModifier" )
                .With( "Modifier", "<Keyboard>/alt" )
                .With( "Button", "<Keyboard>/i" );
            inputAction.performed += ( p ) =>
            {
                ChooseInputFile( );

                if ( !string.IsNullOrEmpty( inputFile ) )
                    ReloadImage( );
            };
            inputAction.Enable( );
            
            inputAction = new InputAction( "ImageOverlayMoveLeft" );
            inputAction.AddCompositeBinding( "ButtonWithOneModifier" )
                .With( "Modifier", "<Keyboard>/ctrl" )
                .With( "Button", "<Keyboard>/leftArrow" );
            inputAction.performed += OnImageOverlayMoveLeft;
            inputAction.Enable();

            inputAction = new InputAction( "ImageOverlayMoveRight" );
            inputAction.AddCompositeBinding( "ButtonWithOneModifier" )
                .With( "Modifier", "<Keyboard>/ctrl" )
                .With( "Button", "<Keyboard>/rightArrow" );
            inputAction.performed += OnImageOverlayMoveRight;
            inputAction.Enable();


            inputAction = new InputAction( "ImageOverlayMoveForward" );
            inputAction.AddCompositeBinding( "ButtonWithOneModifier" )
                .With( "Modifier", "<Keyboard>/ctrl" )
                .With( "Button", "<Keyboard>/upArrow" );
            inputAction.performed += OnImageOverlayMoveForward;
            inputAction.Enable();


            inputAction = new InputAction( "ImageOverlayMoveBackward" );
            inputAction.AddCompositeBinding( "ButtonWithOneModifier" )
                .With( "Modifier", "<Keyboard>/ctrl" )
                .With( "Button", "<Keyboard>/downArrow" );
            inputAction.performed += OnImageOverlayMoveBackward;
            inputAction.Enable();
        }

        private void ChooseInputFile()
        {
            var file = OpenFileDialog.ShowDialog( "Image files\0*.png\0", ".png" );

            if ( !string.IsNullOrEmpty( file ) )
            {
                inputFile = file;
                _config.Image = file;
                MapImageLayerConfig.Save( _config );
            }
        }

        private void InitialiseImageOverlay( )
        {
            lastOverlayCheck = DateTime.Now;

            if ( string.IsNullOrEmpty( inputFile ) )
                ChooseInputFile( );

            if ( !File.Exists( inputFile ) )
                return;

            overlayTexture = new Texture2D( 1, 1, TextureFormat.ARGB32, false );
            overlayTexture.LoadImage( File.ReadAllBytes( inputFile ) );
            overlayTexture.Apply( );

            Debug.Log( "Loaded image: " + Path.GetFileName( inputFile ) );
        }


        public void ReloadImage( )
        {
            InitialiseImageOverlay( );

            if ( overlayTexture != null && renderer != null )
                renderer.material.mainTexture = overlayTexture;
        }

        private void CheckForOverlayChange( )
        {
            if ( string.IsNullOrEmpty( inputFile ) )
                ChooseInputFile( );

            var fileInfo = new FileInfo( inputFile );

            if ( fileInfo.Exists && ( overlayTexture != null && fileInfo.LastWriteTime > lastOverlayCheck || overlayTexture == null ) )
            {
                ReloadImage( );
            }
        }

        private void ToggleImageOverlay( )
        {
            CheckForOverlayChange( );

            if ( overlayTexture == null )
                return;

            ShowImageOverlay = !ShowImageOverlay;

            var soundQuery = GetEntityQuery( ComponentType.ReadOnly<ToolUXSoundSettingsData>( ) );
            soundSettings = soundQuery.GetSingleton<ToolUXSoundSettingsData>( );

            AudioManager.instance.PlayUISound( soundSettings.m_TutorialStartedSound );

            if ( imageOverlayObj == null )
            {
                // Create a new GameObject for Decal Projector
                imageOverlayObj = GameObject.CreatePrimitive( PrimitiveType.Plane );
                imageOverlayObj.name = "ImageOverlay";

                imageOverlayObj.transform.localScale = Vector3.one * 1435f;
                imageOverlayObj.transform.position = Vector3.zero + ( Vector3.up * SampleWaterHeight( ) );

                var position = imageOverlayObj.transform.position;
                var localScale = imageOverlayObj.transform.localScale;

                if ( _config.X.HasValue )
                    position.x = ( float ) _config.X.Value;

                if ( _config.Y.HasValue )
                    position.y = ( float ) _config.Y.Value;

                if ( _config.Z.HasValue )
                    position.z = ( float ) _config.Z.Value;

                if ( _config.ScaleX.HasValue )
                    localScale.x = ( float ) _config.ScaleX.Value;

                if ( _config.ScaleY.HasValue )
                    localScale.z = ( float ) _config.ScaleY.Value;

                imageOverlayObj.transform.localScale = localScale;
                imageOverlayObj.transform.position = position;

                var shader = LoadAssetBundle( );

                if ( shader != null )
                {
                    // Create material for the Decal Projector
                    var mat = new Material( shader );
                    mat.enableInstancing = true;
                    mat.mainTexture = ImageOverlay;
                    mat.SetFloat( "_Transparency", transparency );

                    // Set the material to Decal Projector
                    renderer = imageOverlayObj.GetComponent<Renderer>( );
                    renderer.material = mat;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
            }

            imageOverlayObj.SetActive( ShowImageOverlay );
            Debug.Log( "Image overlay set to: " + ShowImageOverlay );
        }

        private void OnToggleImageOverlay( InputAction.CallbackContext obj )
        {
            ToggleImageOverlay( );
        }

        private void OnImageOverlayIncreaseHeight( InputAction.CallbackContext obj )
        {
            if ( !ShowImageOverlay || imageOverlayObj == null )
                return;

            imageOverlayObj.transform.position += Vector3.up * 1f;
            AudioManager.instance.PlayUISound( soundSettings.m_TransportLineStartSound );

            _config.X = ( decimal ) imageOverlayObj.transform.position.x;
            _config.Y = ( decimal ) imageOverlayObj.transform.position.y;
            _config.Z = ( decimal ) imageOverlayObj.transform.position.z;
            MapImageLayerConfig.Save( _config );
        }

        private void OnImageOverlayDecreaseHeight( InputAction.CallbackContext obj )
        {
            if ( !ShowImageOverlay || imageOverlayObj == null )
                return;

            imageOverlayObj.transform.position -= Vector3.up * 1f;
            AudioManager.instance.PlayUISound( soundSettings.m_TransportLineCompleteSound );

            _config.X = ( decimal ) imageOverlayObj.transform.position.x;
            _config.Y = ( decimal ) imageOverlayObj.transform.position.y;
            _config.Z = ( decimal ) imageOverlayObj.transform.position.z;
            MapImageLayerConfig.Save( _config );
        }

        private void OnImageOverlayIncreaseSize( InputAction.CallbackContext obj )
        {
            if ( !ShowImageOverlay || imageOverlayObj == null )
                return;

            imageOverlayObj.transform.localScale += Vector3.one * 10f;
            AudioManager.instance.PlayUISound( soundSettings.m_PolygonToolDropPointSound );

            _config.ScaleX = ( decimal ) imageOverlayObj.transform.localScale.x;
            _config.ScaleY = ( decimal ) imageOverlayObj.transform.localScale.z;
            MapImageLayerConfig.Save( _config );
        }

        private void OnImageOverlayDecreaseSize( InputAction.CallbackContext obj )
        {
            if ( !ShowImageOverlay || imageOverlayObj == null )
                return;

            imageOverlayObj.transform.localScale -= Vector3.one * 10f;
            AudioManager.instance.PlayUISound( soundSettings.m_PolygonToolDropPointSound );

            _config.ScaleX = ( decimal ) imageOverlayObj.transform.localScale.x;
            _config.ScaleY = ( decimal ) imageOverlayObj.transform.localScale.z;
            MapImageLayerConfig.Save( _config );
        }

        private void OnImageOverlayIncreaseTransparency( InputAction.CallbackContext obj )
        {
            if ( !ShowImageOverlay || imageOverlayObj == null )
                return;

            transparency = Mathf.Clamp( transparency + 0.1f, 0f, 1f );
            renderer.material.SetFloat( "_Transparency", transparency );
            AudioManager.instance.PlayUISound( soundSettings.m_AreaMarqueeStartSound );

            _config.Opacity = ( decimal ) transparency;
            MapImageLayerConfig.Save( _config );
        }

        private void OnImageOverlayDecreaseTransparency( InputAction.CallbackContext obj )
        {
            if ( !ShowImageOverlay || imageOverlayObj == null )
                return;

            transparency = Mathf.Clamp( transparency - 0.1f, 0f, 1f );
            renderer.material.SetFloat( "_Transparency", transparency );
            AudioManager.instance.PlayUISound( soundSettings.m_AreaMarqueeEndSound );

            _config.Opacity = ( decimal ) transparency;
            MapImageLayerConfig.Save( _config );
        }

        private void OnImageOverlayMoveLeft( InputAction.CallbackContext obj )
        {
            if ( !ShowImageOverlay || imageOverlayObj == null )
                return;

            imageOverlayObj.transform.position -= Vector3.right * (float)_config.Speed;
            AudioManager.instance.PlayUISound( soundSettings.m_TransportLineStartSound );

            _config.X = ( decimal ) imageOverlayObj.transform.position.x;
            _config.Y = ( decimal ) imageOverlayObj.transform.position.y;
            _config.Z = ( decimal ) imageOverlayObj.transform.position.z;
            MapImageLayerConfig.Save( _config );
        }

        private void OnImageOverlayMoveRight( InputAction.CallbackContext obj )
        {
            if ( !ShowImageOverlay || imageOverlayObj == null )
                return;

            imageOverlayObj.transform.position += Vector3.right * (float)_config.Speed;
            AudioManager.instance.PlayUISound( soundSettings.m_PolygonToolDropPointSound );

            _config.X = ( decimal ) imageOverlayObj.transform.position.x;
            _config.Y = ( decimal ) imageOverlayObj.transform.position.y;
            _config.Z = ( decimal ) imageOverlayObj.transform.position.z;
            MapImageLayerConfig.Save( _config );
        }

        private void OnImageOverlayMoveForward( InputAction.CallbackContext obj )
        {
            if ( !ShowImageOverlay || imageOverlayObj == null )
                return;

            imageOverlayObj.transform.position += Vector3.forward * (float)_config.Speed;
            AudioManager.instance.PlayUISound( soundSettings.m_AreaMarqueeEndSound );

            _config.X = ( decimal ) imageOverlayObj.transform.position.x;
            _config.Y = ( decimal ) imageOverlayObj.transform.position.y;
            _config.Z = ( decimal ) imageOverlayObj.transform.position.z;
            MapImageLayerConfig.Save( _config );
        }

        private void OnImageOverlayMoveBackward( InputAction.CallbackContext obj )
        {
            if ( !ShowImageOverlay || imageOverlayObj == null )
                return;

            imageOverlayObj.transform.position -= Vector3.forward * (float)_config.Speed;
            AudioManager.instance.PlayUISound( soundSettings.m_TransportLineCompleteSound );

            _config.X = ( decimal ) imageOverlayObj.transform.position.x;
            _config.Y = ( decimal ) imageOverlayObj.transform.position.y;
            _config.Z = ( decimal ) imageOverlayObj.transform.position.z;
            MapImageLayerConfig.Save( _config );
        }

        private Shader LoadAssetBundle( )
        {
            var assembly = Assembly.GetExecutingAssembly( );
            using ( var stream = assembly.GetManifestResourceStream( "MapImageLayer.Resources.additiveshader" ) )
            {
                if ( stream == null )
                {
                    Debug.LogError( "Failed to load embedded resource." );
                    return null;
                }

                var assetBytes = new byte[stream.Length];
                stream.Read( assetBytes, 0, assetBytes.Length );

                var myLoadedAssetBundle = AssetBundle.LoadFromMemory( assetBytes );
                if ( myLoadedAssetBundle == null )
                {
                    Debug.LogError( "Failed to load AssetBundle from memory." );
                    return null;
                }

                var assetNames = myLoadedAssetBundle.GetAllAssetNames( );
                foreach ( var name in assetNames )
                {
                    Debug.Log( name );
                }

                // Load an asset from the bundle
                var loadedShader = myLoadedAssetBundle.LoadAsset<Shader>( "assets/customoverlay.shader" );

                if ( loadedShader == null )
                {
                    Debug.LogError( "Failed to load the customoverlay shader from the AssetBundle." );
                    return null;
                }
                myLoadedAssetBundle.Unload( false );
                return loadedShader;
            }
        }

        private float SampleWaterHeight( )
        {
            var waterSystem = World.GetExistingSystemManaged<WaterSystem>( );
            var terrainSystem = World.GetExistingSystemManaged<TerrainSystem>( );

            var heightData = terrainSystem.GetHeightData( );
            var surfaceData = waterSystem.GetSurfaceData( out _ );
            return ( float ) WaterUtils.SampleHeight( ref surfaceData, ref heightData, Vector3.zero );
        }
    }
}