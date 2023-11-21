using Newtonsoft.Json;
using System.IO;
using UnityEngine;

namespace MapImageLayer
{
    public class MapImageLayerConfig
    {
        public string Image
        {
            get;
            set;
        }

        public decimal? X
        {
            get;
            set;
        }

        public decimal? Y
        {
            get;
            set;
        }

        public decimal? Z
        {
            get;
            set;
        }

        public decimal? ScaleX
        {
            get;
            set;
        }

        public decimal? ScaleY
        {
            get;
            set;
        }

        public decimal Opacity
        {
            get;
            set;
        } = 1m;

        public void SetPosition( Vector3 position )
        {
            X = ( decimal ) position.x; 
            Y = ( decimal ) position.y; 
            Z = ( decimal ) position.z;
        }

        public Vector3 GetPosition( )
        {
            return new Vector3( ( float ) X, ( float ) Y, ( float ) Z );
        }

        public void SetScale( Vector3 scale )
        {
            ScaleX = ( decimal ) scale.x;
            ScaleY = ( decimal ) scale.z;
        }

        public Vector3 GetScale( )
        {
            return new Vector3( ( float ) ScaleX, 1f, ( float ) ScaleY );
        }

        public static void Save( MapImageLayerConfig config )
        {
            var json = JsonConvert.SerializeObject( config );
            var filePath = Path.Combine( GetAssemblyDirectory( ), "config.json" );
            File.WriteAllText( filePath, json );
        }

        public static MapImageLayerConfig Load( )
        {
            var filePath = Path.Combine( GetAssemblyDirectory( ), "config.json" );

            if ( !File.Exists( filePath ) )
                Save( new MapImageLayerConfig( ) );

            var json = File.ReadAllText( filePath );

            return JsonConvert.DeserializeObject<MapImageLayerConfig>( json );
        }

        private static string GetAssemblyDirectory( )
        {
            return Path.GetDirectoryName( typeof( MapImageLayerConfig ).Assembly.Location );
        }
    }
}
