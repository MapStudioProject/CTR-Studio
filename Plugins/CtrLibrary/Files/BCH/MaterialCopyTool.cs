using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SPICA.Formats.CtrGfx;
using SPICA.Formats.CtrH3D;
using SPICA.Formats.CtrH3D.Model.Material;
using SPICA.Math3D;
using SPICA.PICA.Commands;

namespace CtrLibrary
{
    internal class MaterialCopyTool
    {
        public static Dictionary<string, bool> CopyToggles = new Dictionary<string, bool>()
        {
            { "Copy Material/Shader Info", true },
            { "Copy Texture Maps", true },
            { "Copy Texture Coords", true },
            { "Copy Color", true },
            { "Copy Combiners", true },
            { "Copy LUTs", true },
            { "Copy Polygon State", true },
            { "Copy Render State", true },
            { "Copy User Data", true },
        };

        public static void Copy(H3DMaterial material)
        {
            CopyData copied = new CopyData();
            if (CopyToggles["Copy Material/Shader Info"]) copied.MaterialInfo = new MaterialInfo(material);
            if (CopyToggles["Copy Texture Maps"]) copied.TextureData = new TextureData(material);
            if (CopyToggles["Copy Texture Coords"]) copied.TextureCoordData = new TextureCoordData(material);
            if (CopyToggles["Copy Color"]) copied.ColorData = new ColorData(material);
            if (CopyToggles["Copy Combiners"]) copied.CombinerData = new CombinerData(material);
            if (CopyToggles["Copy LUTs"]) copied.LUTData = new LUTData(material);
            if (CopyToggles["Copy Polygon State"]) copied.MaterialRasterData = new MaterialRaster(material);
            if (CopyToggles["Copy Render State"]) copied.RenderState = new RenderState(material);
            if (CopyToggles["Copy User Data"])
            {
                copied.MetaData = material.MaterialParams.MetaData;
                copied.GfxMetaData = material.BcresUserData;
            }

            string json = JsonConvert.SerializeObject(copied, Formatting.Indented);
            ImGuiNET.ImGui.SetClipboardText(json);
        }


        public static void Paste(H3DMaterial material)
        {
            CopyData copied = null;

            try
            {
                string json = ImGuiNET.ImGui.GetClipboardText();
                copied = JsonConvert.DeserializeObject<CopyData>(json);
            }
            catch
            {
                //Possibly invalid copy/paste so skip
                MapStudio.UI.TinyFileDialog.MessageBoxErrorOk("Invalid data pasted into clipboard!");
                return;
            }

            if (copied == null)
                return;

            if (copied.TextureData != null)        copied.TextureData.Paste(material);
            if (copied.TextureCoordData != null)   copied.TextureCoordData.Paste(material);
            if (copied.ColorData != null)          copied.ColorData.Paste(material);
            if (copied.CombinerData != null)       copied.CombinerData.Paste(material);
            if (copied.LUTData != null)            copied.LUTData.Paste(material);
            if (copied.MaterialRasterData != null) copied.MaterialRasterData.Paste(material);
            if (copied.RenderState != null)        copied.RenderState.Paste(material);
            if (copied.MaterialInfo != null)       copied.MaterialInfo.Paste(material);
            if (copied.MetaData != null)
            {
                material.MaterialParams.MetaData = copied.MetaData;
                material.BcresUserData = copied.GfxMetaData;
            }
        }

        class CopyData
        {
            public MaterialInfo MaterialInfo;

            public TextureData TextureData;
            public TextureCoordData TextureCoordData;

            public ColorData ColorData;

            public CombinerData CombinerData;

            public LUTData LUTData;

            public MaterialRaster MaterialRasterData;

            public RenderState RenderState;

            public H3DMetaData MetaData;
            public GfxDict<GfxMetaData> GfxMetaData;
        }

        class MaterialInfo
        {
            public H3DMaterialFlags Flags;
            public string ShaderReference;

            public ushort FogIndex;
            public ushort LightSetIndex;

            public MaterialInfo() { }

            public MaterialInfo(H3DMaterial material)
            {
                Flags = material.MaterialParams.Flags;
                ShaderReference = material.MaterialParams.ShaderReference;
                FogIndex = material.MaterialParams.FogIndex;
                LightSetIndex = material.MaterialParams.LightSetIndex;
            }

            public void Paste(H3DMaterial material)
            {
                material.MaterialParams.Flags = Flags;
                material.MaterialParams.ShaderReference = ShaderReference;
                material.MaterialParams.FogIndex = FogIndex;
                material.MaterialParams.LightSetIndex = LightSetIndex;

            }
        }

        class MaterialRaster
        {
            public PICAFaceCulling FaceCulling;
            public float PolygonOffsetUnit;
            public bool IsPolygonOffsetEnabled;

            public MaterialRaster() { }

            public MaterialRaster(H3DMaterial material)
            {
                FaceCulling = material.MaterialParams.FaceCulling;
                PolygonOffsetUnit = material.MaterialParams.PolygonOffsetUnit;
                IsPolygonOffsetEnabled = material.MaterialParams.IsPolygonOffsetEnabled;
            }

            public void Paste(H3DMaterial material)
            {
                material.MaterialParams.FaceCulling = FaceCulling;
                material.MaterialParams.PolygonOffsetUnit = PolygonOffsetUnit;
                material.MaterialParams.IsPolygonOffsetEnabled = IsPolygonOffsetEnabled;
            }
        }

        public class RenderState
        {
            public int RenderLayer;

            //Alpha
            public PICAAlphaTest AlphaTest;

            //Logic
            public PICALogicalOp LogicalOperation;

            //Blending
            public PICAColorOperation ColorOperation;
            public PICABlendFunction BlendFunction;

            //Stencil
            public PICAStencilTest StencilTest;
            public PICAStencilOperation StencilOperation;

            //Depth
            public PICADepthColorMask DepthColorMask;

            public bool ColorBufferRead;
            public bool ColorBufferWrite;

            public bool StencilBufferRead;
            public bool StencilBufferWrite;

            public bool DepthBufferRead;
            public bool DepthBufferWrite;

            public RenderState() { }

            public RenderState(H3DMaterial material)
            {
                RenderLayer = material.MaterialParams.RenderLayer;
                AlphaTest = material.MaterialParams.AlphaTest;

                LogicalOperation = material.MaterialParams.LogicalOperation;

                ColorOperation = material.MaterialParams.ColorOperation;
                BlendFunction = material.MaterialParams.BlendFunction;

                StencilTest = material.MaterialParams.StencilTest;
                StencilOperation = material.MaterialParams.StencilOperation;

                DepthColorMask = material.MaterialParams.DepthColorMask;

                ColorBufferRead = material.MaterialParams.ColorBufferRead;
                ColorBufferWrite = material.MaterialParams.ColorBufferWrite;

                DepthBufferWrite = material.MaterialParams.DepthBufferWrite;
                DepthBufferRead = material.MaterialParams.DepthBufferRead;

                StencilBufferRead = material.MaterialParams.StencilBufferRead;
                StencilBufferWrite = material.MaterialParams.StencilBufferWrite;
            }

            public void Paste(H3DMaterial material)
            {
                material.MaterialParams.RenderLayer = RenderLayer;
                material.MaterialParams.AlphaTest = AlphaTest;

                material.MaterialParams.LogicalOperation = LogicalOperation;

                material.MaterialParams.ColorOperation = ColorOperation;
                material.MaterialParams.BlendFunction = BlendFunction;

                material.MaterialParams.StencilTest = StencilTest;
                material.MaterialParams.StencilOperation = StencilOperation;

                material.MaterialParams.DepthColorMask = DepthColorMask;

                material.MaterialParams.ColorBufferRead = ColorBufferRead;
                material.MaterialParams.ColorBufferWrite = ColorBufferWrite;

                material.MaterialParams.DepthBufferWrite = DepthBufferWrite;
                material.MaterialParams.DepthBufferRead = DepthBufferRead;

                material.MaterialParams.StencilBufferRead = StencilBufferRead;
                material.MaterialParams.StencilBufferWrite = StencilBufferWrite;
            }
        }

        public class CombinerData
        {
            public PICATexEnvStage[] TexEnvStages;
            public RGBA TexEnvBufferColor;

            public int Constant0Assignment;
            public int Constant1Assignment;
            public int Constant2Assignment;
            public int Constant3Assignment;
            public int Constant4Assignment;
            public int Constant5Assignment;

            public bool UseFragmentLighting;

            public CombinerData() { }

            public CombinerData(H3DMaterial material)
            {
                TexEnvStages = material.MaterialParams.TexEnvStages;
                TexEnvBufferColor = material.MaterialParams.TexEnvBufferColor;
                Constant0Assignment = material.MaterialParams.Constant0Assignment;
                Constant1Assignment = material.MaterialParams.Constant1Assignment;
                Constant2Assignment = material.MaterialParams.Constant2Assignment;
                Constant3Assignment = material.MaterialParams.Constant3Assignment;
                Constant4Assignment = material.MaterialParams.Constant4Assignment;
                Constant5Assignment = material.MaterialParams.Constant5Assignment;
                //Copy over fragment lighting aswell as some stages may need it
                UseFragmentLighting = material.MaterialParams.IsFragmentLightingEnabled;
            }

            public void Paste(H3DMaterial material)
            {
                material.MaterialParams.TexEnvStages = TexEnvStages;
                material.MaterialParams.TexEnvBufferColor = TexEnvBufferColor;
                material.MaterialParams.Constant0Assignment = Constant0Assignment;
                material.MaterialParams.Constant1Assignment = Constant1Assignment;
                material.MaterialParams.Constant2Assignment = Constant2Assignment;
                material.MaterialParams.Constant3Assignment = Constant3Assignment;
                material.MaterialParams.Constant4Assignment = Constant4Assignment;
                material.MaterialParams.Constant5Assignment = Constant5Assignment;
                //Paste over fragment lighting aswell as some stages may need it
                material.MaterialParams.IsFragmentLightingEnabled = UseFragmentLighting;
            }
        }

        public class LUTData
        {
            public PICALUTInAbs LUTInputAbsolute;
            public PICALUTInSel LUTInputSelection;
            public PICALUTInScale LUTInputScale;
            public H3DFresnelSelector FresnelSelector;
            public H3DFragmentFlags FragmentFlags;

            public H3DTranslucencyKind Layer;

            public string LUTDist0TableName;
            public string LUTDist1TableName;
            public string LUTFresnelTableName;
            public string LUTReflecRTableName;
            public string LUTReflecGTableName;
            public string LUTReflecBTableName;

            public string LUTDist0SamplerName;
            public string LUTDist1SamplerName;
            public string LUTFresnelSamplerName;
            public string LUTReflecRSamplerName;
            public string LUTReflecGSamplerName;
            public string LUTReflecBSamplerName;

            public LUTData() { }

            public LUTData(H3DMaterial material)
            {
                Layer = material.MaterialParams.TranslucencyKind;

                LUTInputAbsolute = material.MaterialParams.LUTInputAbsolute;
                LUTInputSelection = material.MaterialParams.LUTInputSelection;
                LUTInputScale = material.MaterialParams.LUTInputScale;
                FresnelSelector = material.MaterialParams.FresnelSelector;
                FragmentFlags = material.MaterialParams.FragmentFlags;

                LUTDist0TableName = material.MaterialParams.LUTDist0TableName;
                LUTDist1TableName = material.MaterialParams.LUTDist1TableName;
                LUTFresnelTableName = material.MaterialParams.LUTFresnelTableName;
                LUTReflecRTableName = material.MaterialParams.LUTReflecRTableName;
                LUTReflecGTableName = material.MaterialParams.LUTReflecGTableName;
                LUTReflecBTableName = material.MaterialParams.LUTReflecBTableName;

                LUTDist0SamplerName = material.MaterialParams.LUTDist0SamplerName;
                LUTDist1SamplerName = material.MaterialParams.LUTDist1SamplerName;
                LUTFresnelSamplerName = material.MaterialParams.LUTFresnelSamplerName;
                LUTReflecRSamplerName = material.MaterialParams.LUTReflecRSamplerName;
                LUTReflecGSamplerName = material.MaterialParams.LUTReflecGSamplerName;
                LUTReflecBSamplerName = material.MaterialParams.LUTReflecBSamplerName;
            }

            public void Paste(H3DMaterial material)
            {
                material.MaterialParams.TranslucencyKind = Layer;

                material.MaterialParams.LUTInputAbsolute = LUTInputAbsolute;
                material.MaterialParams.LUTInputSelection = LUTInputSelection;
                material.MaterialParams.LUTInputScale = LUTInputScale;
                material.MaterialParams.FresnelSelector = FresnelSelector;
                material.MaterialParams.FragmentFlags = FragmentFlags;

                material.MaterialParams.LUTDist0TableName = LUTDist0TableName;
                material.MaterialParams.LUTDist1TableName = LUTDist1TableName;
                material.MaterialParams.LUTFresnelTableName = LUTFresnelTableName;
                material.MaterialParams.LUTReflecRTableName = LUTReflecRTableName;
                material.MaterialParams.LUTReflecGTableName = LUTReflecGTableName;
                material.MaterialParams.LUTReflecBTableName = LUTReflecBTableName;

                material.MaterialParams.LUTDist0SamplerName = LUTDist0SamplerName;
                material.MaterialParams.LUTDist1SamplerName = LUTDist1SamplerName;
                material.MaterialParams.LUTFresnelSamplerName = LUTFresnelSamplerName;
                material.MaterialParams.LUTReflecRSamplerName = LUTReflecRSamplerName;
                material.MaterialParams.LUTReflecGSamplerName = LUTReflecGSamplerName;
                material.MaterialParams.LUTReflecBSamplerName = LUTReflecBSamplerName;
            }
        }

        public class ColorData
        {
            public RGBA EmissionColor;
            public RGBA AmbientColor;
            public RGBA DiffuseColor;
            public RGBA Specular0Color;
            public RGBA Specular1Color;
            public RGBA Constant0Color;
            public RGBA Constant1Color;
            public RGBA Constant2Color;
            public RGBA Constant3Color;
            public RGBA Constant4Color;
            public RGBA Constant5Color;
            public RGBA BlendColor;

            public float VertexColorScale;


            public ColorData() { }

            public ColorData(H3DMaterial material)
            {
                Constant0Color = material.MaterialParams.Constant0Color;
                Constant1Color = material.MaterialParams.Constant1Color;
                Constant2Color = material.MaterialParams.Constant2Color;
                Constant3Color = material.MaterialParams.Constant3Color;
                Constant4Color = material.MaterialParams.Constant4Color;
                Constant5Color = material.MaterialParams.Constant5Color;
                EmissionColor= material.MaterialParams.EmissionColor;
                DiffuseColor = material.MaterialParams.DiffuseColor;
                Specular0Color = material.MaterialParams.Specular0Color;
                Specular1Color = material.MaterialParams.Specular1Color;
                BlendColor = material.MaterialParams.BlendColor;
                VertexColorScale = material.MaterialParams.ColorScale;
            }

            public void Paste(H3DMaterial material)
            {
                material.MaterialParams.Constant0Color = Constant0Color;
                material.MaterialParams.Constant1Color = Constant1Color;
                material.MaterialParams.Constant2Color = Constant2Color;
                material.MaterialParams.Constant3Color = Constant3Color;
                material.MaterialParams.Constant4Color = Constant4Color;
                material.MaterialParams.Constant5Color = Constant5Color;
                material.MaterialParams.EmissionColor = EmissionColor;
                material.MaterialParams.DiffuseColor = DiffuseColor;
                material.MaterialParams.Specular0Color = Specular0Color;
                material.MaterialParams.Specular1Color = Specular1Color;
                material.MaterialParams.BlendColor = BlendColor;
                material.MaterialParams.ColorScale = VertexColorScale;

            }
        }

        [JsonObject()]
        class TextureCoordData
        {
            public H3DTextureCoord[] TextureCoords;
            public float[] Sources;
            public H3DTexCoordConfig TexCoordConfig;

            public TextureCoordData() { }

            public TextureCoordData(H3DMaterial material)
            {
                TextureCoords = material.MaterialParams.TextureCoords;
                Sources = material.MaterialParams.TextureSources;
                TexCoordConfig = material.MaterialParams.TexCoordConfig;
            }

            public void Paste(H3DMaterial material)
            {
                material.MaterialParams.TextureCoords = TextureCoords;
                material.MaterialParams.TextureSources = Sources;
                material.MaterialParams.TexCoordConfig = TexCoordConfig;
            }
        }

        [JsonObject()]
        class TextureData
        {
            public bool[] Enabled;
            public string Texture0;
            public string Texture1;
            public string Texture2;
            public H3DTextureMapper[] TextureMaps;
            public H3DBumpMode BumpMode;
            public byte BumpTexture;

            public TextureData() { }

            public TextureData(H3DMaterial material)
            {
                Enabled = material.EnabledTextures;
                Texture0 = material.Texture0Name;
                Texture1 = material.Texture1Name;
                Texture2 = material.Texture2Name;
                TextureMaps = material.TextureMappers;
                BumpMode = material.MaterialParams.BumpMode;
                BumpTexture = material.MaterialParams.BumpTexture;
            }

            public void Paste(H3DMaterial material)
            {
                material.EnabledTextures = Enabled;
                material.Texture0Name = Texture0;
                material.Texture1Name = Texture1;
                material.Texture2Name = Texture2;
                material.TextureMappers = TextureMaps;
                material.MaterialParams.BumpMode = BumpMode;
                material.MaterialParams.BumpTexture = BumpTexture;
            }
        }
    }
}
