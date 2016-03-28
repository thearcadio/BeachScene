using UnityEngine;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace ErosionBrushPlugin
{
	[System.Serializable]
	public class Preset
	{
		//splat preset
		[System.Serializable]
		public struct SplatPreset
		{
			public bool apply;
			public float opacity;
			public int num;
		}

		//main brush params
		public float brushSize = 50;
		public float brushFallof = 0.6f;
		public float brushSpacing = 0.15f;
		public int downscale = 1;
		public float blur = 0.1f;
		public bool preserveDetail = false;

		public bool isErosion;
		public bool isNoise { get{return !isErosion;} set{isErosion=!value;} }

		//noise brush
		public int noise_seed = 12345;
		public float noise_amount = 20f;
		public float noise_size = 200f;
		public float noise_detail = 0.55f;
		public float noise_uplift = 0.8f;
		public float noise_ruffle = 1f;

		//erosion brush
		public int erosion_iterations = 3;
		public float erosion_durability = 0.9f;
		public int erosion_fluidityIterations = 3;
		public float erosion_amount = 1f; //quantity of erosion made by iteration. Lower values require more iterations, but will give better results
		public float sediment_amount = 0.8f; //quantity of sediment that was raised by erosion will drop back to land. Lower values will give eroded canyons with washed-out land, but can produce artefacts
		public float wind_amount = 0.75f;
		public float erosion_smooth = 0.15f;
		public float ruffle = 0.1f;

		//painting
		public SplatPreset foreground = new SplatPreset() { opacity=1 };
		public SplatPreset background = new SplatPreset() { opacity=1 };
		public bool paintSplat
		{get{
			return  (foreground.apply && foreground.opacity>0.01f) ||
					(background.apply && background.opacity>0.01f);
		}}
		
		//save-load
		public string name;
		public bool saveBrushSize;
		public bool saveBrushParams;
		public bool saveErosionNoiseParams;
		public bool saveSplatParams;
		public Preset Copy() { return (Preset) this.MemberwiseClone(); }
	}
	
	[ExecuteInEditMode]
	public class ErosionBrush : MonoBehaviour 
	{
		private Terrain _terrain;
		public Terrain terrain { get{ if (_terrain==null) _terrain=GetComponent<Terrain>(); return _terrain; } set {_terrain=value;} }

		public Preset preset = new Preset(); 
		public Preset[] presets = new Preset[0];
		public int guiSelectedPreset = 0;

		public bool paint = false;
		public bool wasPaint = false;
		public bool moveDown = false;

		public Transform moveTfm;
		public bool gen;

		public bool undo; 

		[System.NonSerialized] public Texture2D guiHydraulicIcon;
		[System.NonSerialized] public Texture2D guiWindIcon;
		[System.NonSerialized] public Texture2D guiPluginIcon;
		public int guiApplyIterations = 1;
		public int[] guiChannels;
		public string[] guiChannelNames;
		public Color guiBrushColor = new Color(1f,0.7f,0.3f);
		public float guiBrushThickness = 4;
		public int guiBrushNumCorners = 32;
		public bool recordUndo = true;
		public bool unity5positioning = false;
		public bool focusOnBrush = true;

		public bool guiShowPreset = true;
		public bool guiShowBrush = true;
		public bool guiShowGenerator = true;
		public bool guiShowTextures = true;
		public bool guiShowGlobal = false;
		public bool guiShowSettings = false;
		public bool guiShowAbout = false;
		public int guiMaxBrushSize = 100;
		public bool guiSelectPresetsUsingNumkeys = true;

		[System.NonSerialized] Matrix srcHeight = new Matrix( new CoordRect(0,0,0,0) );
		//[System.NonSerialized] Matrix srcCliff = new Matrix( new CoordRect(0,0,0,0) );
		//[System.NonSerialized] Matrix srcSediment = new Matrix( new CoordRect(0,0,0,0) );

		[System.NonSerialized] Matrix wrkHeight = new Matrix( new CoordRect(0,0,0,0) );
		[System.NonSerialized] Matrix wrkCliff = new Matrix( new CoordRect(0,0,0,0) );
		[System.NonSerialized] Matrix wrkSediment = new Matrix( new CoordRect(0,0,0,0) );

		[System.NonSerialized] Matrix dstHeight = new Matrix( new CoordRect(0,0,0,0) );
		[System.NonSerialized] Matrix dstCliff = new Matrix( new CoordRect(0,0,0,0) );
		[System.NonSerialized] Matrix dstSediment = new Matrix( new CoordRect(0,0,0,0) );


		public void ApplyBrush (Rect worldRect, bool useFallof=true, bool newUndo=false)
		{
			//preparing useful values
			TerrainData data = terrain.terrainData;
			bool paintSplat = preset.foreground.apply || preset.background.apply;
			if (data.alphamapLayers==0) paintSplat = false;


			//finding working rects
			int heightRes = data.heightmapResolution-1; int splatRes = data.alphamapResolution;
			CoordRect heightRect = new CoordRect(worldRect.x*heightRes, worldRect.y*heightRes, worldRect.width*heightRes, worldRect.height*heightRes);
			CoordRect splatRect = new CoordRect(worldRect.x*splatRes, worldRect.y*splatRes, worldRect.width*splatRes, worldRect.height*splatRes);
			CoordRect workRect = heightRect / preset.downscale;


			//loading height
			srcHeight.ChangeRect(heightRect); //Matrix srcHeight = new Matrix(heightRect);

			CoordRect heightArrayRect = CoordRect.Intersect(heightRect, new CoordRect(0,0,data.heightmapResolution, data.heightmapResolution));
			float[,] heightArray = data.GetHeights(heightArrayRect.offset.x, heightArrayRect.offset.z, heightArrayRect.size.x, heightArrayRect.size.z); //think of heightArray as a Matrix with heightArrayRect
			
			CoordRect intersection = CoordRect.Intersect(heightRect, heightArrayRect);
			Coord min = intersection.Min; Coord max = intersection.Max;
			for (int x=min.x; x<max.x; x++)
				for (int z=min.z; z<max.z; z++)
					srcHeight[x,z] = heightArray[z-heightArrayRect.offset.z,x-heightArrayRect.offset.x];
			srcHeight.RemoveBorders(intersection);


			//getting splats (this should be done before undo)
			CoordRect splatArrayRect = CoordRect.Intersect(splatRect, new CoordRect(0,0,data.alphamapResolution, data.alphamapResolution));
			float[,,] splatArray = data.GetAlphamaps(splatArrayRect.offset.x, splatArrayRect.offset.z, splatArrayRect.size.x, splatArrayRect.size.z);


			//record undo. Undo.RecordObject and SetDirty are done in editor
			if (recordUndo) 
			{
				if (newUndo)
				{
					if (undoList.Count > 10) undoList.RemoveAt(0);
					undoList.Add(new List<UndoStep>());
				}
				if (undoList.Count == 0) undoList.Add(new List<UndoStep>());
				undoList[undoList.Count-1].Add( new UndoStep(heightArray, splatArray, heightArrayRect.offset.x, heightArrayRect.offset.z, splatArrayRect.offset.x, splatArrayRect.offset.z) );
			}


			//downscaling src to work rect
			wrkHeight = srcHeight.Resize(workRect, wrkHeight); //Matrix wrkHeight = srcHeight.Resize(workRect);
			wrkCliff.ChangeRect(workRect); wrkCliff.Clear();  //Matrix wrkCliff = new Matrix(workRect);
			wrkSediment.ChangeRect(workRect); wrkSediment.Clear(); //Matrix wrkSediment = new Matrix(workRect);


			//generating
			#if UNITY_EDITOR
			if (!preset.isErosion) Noise.NoiseIteration(wrkHeight, wrkCliff, wrkSediment,
				size:preset.noise_size, intensity:preset.noise_amount, detail:preset.noise_detail, seed:preset.noise_seed, uplift:preset.noise_uplift, maxHeight:data.size.y);
			else
			{
				Erosion.ErosionIteration(wrkHeight, wrkCliff, wrkSediment,
					erosionDurability:preset.erosion_durability, erosionAmount:preset.erosion_amount, sedimentAmount:preset.sediment_amount, erosionFluidityIterations:preset.erosion_fluidityIterations, ruffle:preset.ruffle);
			
				//blurring heights
				wrkHeight.Blur(intensity:preset.erosion_smooth);

				//multiply splats to make them look as close as the previous version
				wrkCliff.Multiply(120f);
				wrkSediment.Multiply(5f);
			}
			#endif


			//upscaling result to dst
			dstHeight = wrkHeight.Resize(heightRect, dstHeight); //Matrix dstHeight = wrkHeight.Resize(heightRect);
			if (preset.downscale != 1) dstHeight.Blur(intensity:preset.downscale/4);

			dstCliff = wrkCliff.Resize(splatRect, dstCliff); //Matrix dstCliff = wrkCliff.Resize(splatRect);
			dstSediment = wrkSediment.Resize(splatRect, dstSediment); //Matrix dstSediment = wrkSediment.Resize(splatRect);

			//preserving height detail
			if (preset.downscale != 1 && preset.preserveDetail)
			{
				Matrix blrHeight = srcHeight.Copy();
				blrHeight = blrHeight.Resize(workRect);
				blrHeight = blrHeight.Resize(heightRect);
				blrHeight.Blur(intensity:preset.downscale/4);
				for (int i=0; i<dstHeight.count; i++) 
				{
					float delta = srcHeight.array[i] - blrHeight.array[i];
					dstHeight.array[i] += delta;
				}
			}


			//apply fallof
			min = heightRect.Min; max = heightRect.Max; Coord center = heightRect.Center; float radius = heightRect.size.x/2f;
			for (int x = min.x; x<max.x; x++)
				for (int z = min.z; z<max.z; z++)
			{
				float percent = (radius - Coord.Distance(new Coord(x,z), center)) / (radius-radius*preset.brushFallof);
				if (percent < 0) percent = 0; if (percent > 1) percent = 1;
				percent = 3*percent*percent - 2*percent*percent*percent;

				dstHeight[x,z] = srcHeight[x,z]*(1-percent) + dstHeight[x,z]*percent;
			}

			min = splatRect.Min; max = splatRect.Max; center = splatRect.Center; radius = splatRect.size.x/2f;
			for (int x = min.x; x<max.x; x++)
				for (int z = min.z; z<max.z; z++)
			{
				float percent = (radius - Coord.Distance(new Coord(x,z), center)) / (radius-radius*preset.brushFallof);
				if (percent < 0) percent = 0; if (percent > 1) percent = 1;
				percent = 3*percent*percent - 2*percent*percent*percent;

				dstCliff[x,z] *= percent; dstSediment[x,z] *= percent;
			}


			//setting height
			intersection = CoordRect.Intersect(heightRect, heightArrayRect);
			min = intersection.Min; max = intersection.Max;
			for (int x=min.x; x<max.x; x++)
				for (int z=min.z; z<max.z; z++)
					heightArray[z-heightArrayRect.offset.z,x-heightArrayRect.offset.x] = dstHeight[x,z];

			data.SetHeightsDelayLOD(heightArrayRect.offset.x, heightArrayRect.offset.z, heightArray);


			//setting splats
			if (paintSplat)
			{
				intersection = CoordRect.Intersect(splatRect, splatArrayRect);
				min = intersection.Min; max = intersection.Max;
			
				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
						splatArray[z-splatArrayRect.offset.z,x-splatArrayRect.offset.x, preset.foreground.num] += dstCliff[x,z];
				splatArray.Normalize(preset.foreground.num);

				for (int x=min.x; x<max.x; x++)
					for (int z=min.z; z<max.z; z++)
					splatArray[z-splatArrayRect.offset.z,x-splatArrayRect.offset.x, preset.background.num] += dstSediment[x,z];
				splatArray.Normalize(preset.background.num);

				data.SetAlphamaps(splatArrayRect.offset.x, splatArrayRect.offset.z, splatArray);
			}
		}


		public struct UndoStep
		{
			float[,] heights;
			int heightsOffsetX; int heightsOffsetZ;
			float[,,] splats;
			int splatsOffsetX; int splatsOffsetZ;

			public UndoStep (float[,] heights, float[,,] splats, int heightsOffsetX, int heightsOffsetZ, int splatsOffsetX, int splatsOffsetZ)
			{
				//clamping offset low (no need to clamp high as float[,] already has proper size)
				if (heightsOffsetX<0) heightsOffsetX=0; if (heightsOffsetZ<0) heightsOffsetZ=0; 
				if (splatsOffsetX<0) splatsOffsetX=0; if (splatsOffsetZ<0) splatsOffsetZ=0; 
				
				this.heightsOffsetX = heightsOffsetX; this.heightsOffsetZ = heightsOffsetZ;
				this.splatsOffsetX = splatsOffsetX; this.splatsOffsetZ = splatsOffsetZ;
				this.heights = heights.Clone() as float[,]; 
				if (splats!=null) this.splats = splats.Clone() as float[,,];
				else this.splats = null;
			}

			public void Perform (TerrainData data)
			{
				data.SetHeights(heightsOffsetX,heightsOffsetZ,heights);
				if (splats!=null) data.SetAlphamaps(splatsOffsetX,splatsOffsetZ,splats);
			}
		}
		public List< List<UndoStep> > undoList = new List< List<UndoStep> >();	
		public bool allowUndo;

	}
}//namespace



