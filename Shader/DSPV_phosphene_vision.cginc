#ifndef DSPV_PHOSPHENE_VISION
// Upgrade NOTE: excluded shader from DX11, OpenGL ES 2.0 because it uses unsized arrays
#pragma exclude_renderers d3d11 gles
// // Upgrade NOTE: excluded shader from DX11 because it uses wrong array syntax (type[size] name)
// #pragma exclude_renderers d3d11
    #define DSPV_PHOSPHENE_VISION

    struct Phosphene
    {
        float2 position;
        float size;
        float2 activation;
        float2 trace;
    };


    // Pseudo-random noise generators (not used for now)
    uint umu7_wang_hash(uint key)
    {
        uint hash = (key ^ 61) ^(key >> 16);

        hash += hash << 3;
        hash ^= hash >> 4;
        hash *= 0x27d4eb2d;
        hash ^= hash >> 15;

        return hash;
    }

    fixed4 DSPV_gaussian(float r, float sigma) {
        float c = 1 / (sigma * 3.4954077);
        return c * exp(-(r * r) / (2 * sigma * sigma));
    }


    float4 DSPV_phospheneSimulation(
      StructuredBuffer<Phosphene> phospheneBuffer,
      int gazeLocked,
      float2 eyePosition,
      float nPhosphenes, 
      float sizeCoefficient, 
      float brightness, 
      float2 pixelPosition
    ) {
        // Output luminance for current pixel
        fixed4 pixelLuminance = fixed4(0,0,0,0);

        // Distance of current pixel to phosphene center
        float phospheneDistance;

        // Phosphene characteristics (read from shared phosphenes databuffer)
        float2 phospheneCenter;
        float phospheneSize;
        float phospheneActivation;

        float sqrtN = sqrt(nPhosphenes);

        // Loop over all phosphenes
        for (int i = 0; i<nPhosphenes; i++){

          /// Read phosphene from databuffer
          phospheneSize = phospheneBuffer[i].size;
          phospheneCenter = phospheneBuffer[i].position;
          phospheneActivation = phospheneBuffer[i].activation.x;

          // Adjust position of phosphene relative to the gaze direction
          if (gazeLocked == 1){
            phospheneCenter += eyePosition - 0.5;
          }

          // Calculate distance to current pixel (only the activity of nearby
          // phosphenes have an effect on the current pixel intensity).
          phospheneDistance = distance(phospheneCenter, pixelPosition);

          // Rule of thumb:
          // at a distacnce of > 3 sigmas, the tail of the gaussian is unobservable
          if (phospheneDistance < 3 * phospheneSize) {
            // Add the effect of the phosphene to the luminance of the current pixel
            pixelLuminance += phospheneActivation * DSPV_gaussian(phospheneDistance, phospheneSize);
          }

        }

        return brightness * pixelLuminance; //   tex2D(phospheneMapping,xgrid +(0.5/36)); //
    }

#endif
