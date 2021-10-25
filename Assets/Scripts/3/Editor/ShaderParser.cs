/*
Referenced/Inspired from/by https://github.com/d4rkc0d3r/d4rkAvatarOptimizer and https://github.com/lyuma/LyumaShader
 
MIT License

Copyright (c) 2021 d4rkpl4y3r

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.



Copyright 2018-2021 Lyuma

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */



#region
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
#endregion
namespace _3.Editor
{
    //TODO Check if Commented Out
    public static class ShaderExtensions
    {
        /// <summary>
        ///     Get the start of the Property Block
        /// </summary>
        /// <param name="Shader">Shader</param>
        /// <returns>
        ///     returns the line at which the Property Block starts (counting from 0)
        ///     returns -1 if there is no Property Block
        /// </returns>
        public static int GetStartOfPropertyBlock(this Shader shader)
        {
            string path = AssetDatabase.GetAssetPath(shader);
            string[] shaderData = File.ReadAllLines(path);
            int properties = -1;
            int lineNum = -1;
            foreach (string line in shaderData)
            {
                lineNum++;
                properties = line.IndexOf("Properties", StringComparison.CurrentCulture);
                if (properties != -1)
                    return lineNum;
            }
            Debug.Log("no Property Block was found");
            return -1;
        }
        /// <summary>
        ///     Get the end of the Property Block
        /// </summary>
        /// <param name="shader">Shader</param>
        /// <returns>
        ///     returns the line at which the Property Block ends (counting from 0),
        ///     returns -1 if there is no property Block
        /// </returns>
        public static int GetEndOfPropertyBlock(this Shader shader)
        {
            string path = AssetDatabase.GetAssetPath(shader);
            string[] shaderData = File.ReadAllLines(path);
            int propertiesStart = shader.GetStartOfPropertyBlock();
            if (propertiesStart == -1)
            {
                return -1;
            }
            int lineNum = -1;
            bool enteredBlock = false;
            int bracketLevel = 0;
            foreach (string line in shaderData)
            {
                lineNum++;
                if (lineNum >= propertiesStart)
                {
                    int openBrackets = line.IndexOf("{", StringComparison.CurrentCulture);
                    int closingBrackets = line.IndexOf("}", StringComparison.CurrentCulture);

                    if (openBrackets != -1)
                    {
                        bracketLevel++;
                        enteredBlock = true;
                    }
                    if (closingBrackets != -1)
                        bracketLevel--;
                    if (bracketLevel == 0 && enteredBlock)
                        return lineNum;
                }
            }
            return -1;
        }
        /// <summary>
        ///     Get the line at which each Pass starts
        /// </summary>
        /// <param name="shader">Shader</param>
        /// <returns>
        ///     returns a int[] where the index is the number of the Pass and the value is the line at which it starts (counting from 0),
        ///     returns int[1] with value -1 if no Pass is found
        /// </returns>
        public static int[] GetPassLines(this Shader shader)
        {
            string path = AssetDatabase.GetAssetPath(shader);
            string[] shaderData = File.ReadAllLines(path);
            int passes = shader.passCount;
            int[] passLines;
            if (passes == 0)
            {
                passLines = new int[1];
                passLines[0] = -1;
                return passLines;
            }
            passLines = new int[passes];
            int pass = 0;
            int grabPass = -1;
            int lineNum = -1;
            foreach (string line in shaderData)
            {
                //TODO Check if After SubShader 
                //TODO Check for GrabPass
                lineNum++;
                int _pass = line.IndexOf("Pass", StringComparison.CurrentCulture);
                if (_pass != -1)
                {
                    passLines[pass] = lineNum;
                    pass++;
                }
                if (pass == passes)
                {
                    return passLines;
                }
            }
            Debug.Log("no Pass was found");
            return passLines;
        }
    }
}
