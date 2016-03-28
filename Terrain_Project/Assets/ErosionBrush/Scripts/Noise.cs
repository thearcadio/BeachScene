using UnityEngine;
using System.Collections;

namespace ErosionBrushPlugin 
{
	public class Noise
	{
		public static int seed { get{return Random.seed;} set{Random.seed = value;} }

		public static void NoiseIteration (Matrix heightsMatrix,  Matrix cliffMatrix, Matrix sedimentsMatrix, float size, float amount, float uplift, float maxHeight)
		{
			Coord min = heightsMatrix.rect.Min; Coord max = heightsMatrix.rect.Max; 
			for (int x=min.x; x<max.x; x++)
				for (int z=min.z; z<max.z; z++)
			{
				float noise = Noise.Fractal(x, z, size);
				//noise = 1f*(x-min.x)/(max.x-min.x);
				noise = (noise-(1-uplift)) * amount;
				heightsMatrix[x,z] += noise / maxHeight;

				//writing cliff
				if (cliffMatrix != null)
				{
					float splatNoise = Mathf.Max(0,noise);
					cliffMatrix[x,z] = splatNoise*0.1f; //Mathf.Sqrt(splatNoise)*0.3f;
				}

				//writing sediment
				if (sedimentsMatrix != null)
				{
					float sedimentNoise = Mathf.Max(0,-noise);
					sedimentsMatrix[x,z] = sedimentNoise*0.1f; //Mathf.Sqrt(sedimentNoise)*0.3f;
				}
			}
		}

		public static void NoiseIteration (Matrix heightMatrix,  Matrix cliffMatrix, Matrix sedimentsMatrix,  
			float size, float intensity=1, float detail=0.5f, Vector2 offset=new Vector2(), int seed=12345, float uplift=0.5f, float maxHeight=500)
		{
			int step = (int)(4096f / heightMatrix.rect.size.x);

			int totalSeedX = ((int)offset.x + seed*7) % 77777;
			int totalSeedZ = ((int)offset.y + seed*3) % 73333;

			//get number of iterations
			int numIterations = 1; //max size iteration included
			float tempSize = size;
			for (int i=0; i<100; i++)
			{
				tempSize = tempSize/2;
				if (tempSize<1) break;
				numIterations++;
			}

			//making some noise
			Coord min = heightMatrix.rect.Min; Coord max = heightMatrix.rect.Max;
			for (int x=min.x; x<max.x; x++)
			{
				for (int z=min.z; z<max.z; z++)
				{
					float result = 0.5f;
					float curSize = size*10;
					float curAmount = 1;
				
					//applying noise
					for (int i=0; i<numIterations;i++)
					{
						float perlin = Mathf.PerlinNoise(
						(x + totalSeedX + 1000*(i+1))*step/(curSize+1), 
						(z + totalSeedZ + 100*i)*step/(curSize+1) );
						perlin = (perlin-0.5f)*curAmount + 0.5f;

						//applying overlay
						if (perlin > 0.5f) result = 1 - 2*(1-result)*(1-perlin);
						else result = 2*perlin*result;

						curSize *= 0.5f;
						curAmount *= detail; //detail is 0.5 by default
					}

					if (result < 0) result = 0;
					if (result > 1) result = 1;
					float noise = (result-(1-uplift)) * intensity;


					heightMatrix[x,z] += noise / maxHeight;
					
					//writing cliff
					if (cliffMatrix != null)
						cliffMatrix[x,z] = noise>0? noise*0.1f : 0; //Mathf.Sqrt(splatNoise)*0.3f;

					//writing sediment
					if (sedimentsMatrix != null)
						sedimentsMatrix[x,z] = noise<0? -noise*0.1f : 0; //Mathf.Sqrt(sedimentNoise)*0.3f;

				}
			}
		}
		
		public static float Fractal (int x, int z, float size, float detail=0.5f)
		{
			//x+=1000
		
			float result = 0.5f;
			float curSize = size;
			float curAmount = 1;

			//get number of iterations
			int numIterations = 1; //max size iteration included
			for (int i=0; i<100; i++)
			{
				curSize = curSize/2;
				if (curSize<1) break;
				numIterations++;
			}

			//applying noise
			curSize = size;
			for (int i=0; i<numIterations;i++)
			{	
				float perlin = Mathf.PerlinNoise(x/(curSize+1), z/(curSize+1));
				perlin = (perlin-0.5f)*curAmount + 0.5f;

				//applying overlay
				if (perlin > 0.5f) result = 1 - 2*(1-result)*(1-perlin); //(1 - (1-2*(perlin-0.5f)) * (1-result));
				else result = 2*perlin*result;

				curSize *= 0.5f;
				curAmount *= detail; //detail is 0.5 by default
			}

			if (result < 0) result = 0;
			if (result > 1) result = 1;
				
			return result;
		}
	}
}