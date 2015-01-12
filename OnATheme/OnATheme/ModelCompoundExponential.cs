﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Newtonsoft.Json;

namespace OnATheme
{
    public class ModelCompoundExponential : ModelCompound
    {
        List<TextureGroup> _textureGroups = new List<TextureGroup>();

        /// <summary>
        /// A model with permutations
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Parent"></param>
        /// <param name="RandTextures"></param>
        /// <param name="RandFaces"></param>
        /// <param name="ConstTextures"></param>
        /// <param name="XRotation"></param>
        /// <param name="YRotation"></param>
        public ModelCompoundExponential(string Name, string Parent, List<TextureGroup> TextureGroups, bool[] XRotation, bool[] YRotation)
            :base(Name,Parent,XRotation,YRotation)
        {
            _textureGroups = TextureGroups;
        }
        /// <summary>
        /// Total number of models that will be created by this class
        /// </summary>
        /// <returns></returns>
        private int NumModels()
        {
            int numModels = 1;

            foreach (TextureGroup tg in _textureGroups)
                numModels *= (int)Math.Pow(tg.Textures.Count, tg.Faces.Count);

            return numModels;
        }
        /// <summary>
        /// Write the model files for the model
        /// </summary>
        public override void WriteModel()
        {
            int _modelNo = 0, _groupNo = 0;
            List<Texture> _modelTextures = new List<Texture>();

            // Add textures
            
            WriteModelRecur(_modelTextures, 0, ref _modelNo, _groupNo);

            // Force the garbage collection.
            // Since there could be a lot of objects getting destroyed,
            // I thought I'd better do this.
            GC.Collect();
        }
        /// <summary>
        /// Create the model files for all of the possible combinations
        /// </summary>
        /// <param name="ModelTextures"></param>
        /// <param name="UnusedSides"></param>
        /// <param name="modelNo"></param>
        private void WriteModelRecur(List<Texture> ModelTextures, int Face, ref int modelNo, int groupNo)
        {
            if (groupNo < _textureGroups.Count)
            {
                if (Face < _textureGroups[groupNo].Faces.Count)
                {
                    for (int i = 0; i < _textureGroups[groupNo].Textures.Count; i++)
                    {
                        ModelTextures.Add(new Texture(_textureGroups[groupNo].Faces[Face], _textureGroups[groupNo].Textures[i]));
                        if (Face >= _textureGroups[groupNo].Faces.Count - 1)
                            WriteModelRecur(ModelTextures, 0, ref modelNo, groupNo + 1);
                        else
                            WriteModelRecur(ModelTextures, Face + 1, ref modelNo, groupNo);
                        ModelTextures.RemoveAt(ModelTextures.Count - 1);
                    }
                }
            }
            else
            {
                JsonWriter w;
                // Write the model
                if (modelNo == 0 && _parent != _name)
                    w = new JsonTextWriter(File.CreateText(@"OaT/assets/minecraft/models/block/" + _name + ".json"));
                else
                    w = new JsonTextWriter(File.CreateText(@"OaT/assets/minecraft/models/block/" + _name + "_" + modelNo.ToString() + ".json"));

                w.Formatting = Formatting.Indented;

                w.WriteStartObject();
                w.WritePropertyName("parent");
                w.WriteValue(MODEL_PATH + _parent);
                w.WritePropertyName("textures");

                w.WriteStartObject();
                foreach (Texture t in ModelTextures)
                    t.WriteTextureJSON(w);
                w.WriteEndObject();

                w.WriteEndObject();

                w.Close();
                modelNo++;
                groupNo--;
            }
        }
        /// <summary>
        /// Write the blockstates file for the model
        /// </summary>
        /// <param name="w"></param>
        public override void WriteBlockstate(JsonWriter w)
        {
            // The loops and ifs are used to decide whether or not to write this variant as rotated.
            // Currently, thre is no way to specify different weights for each rotation (apart from manual editing)
            // To do so would be a pain to do. Hence, I'm not doing it. (right now, that is)
            for (int i = 0; i < 4; i++)
                if (_xRot[i])
                    for (int j = 0; j < 4; j++)
                        if (_yRot[j])
                        {
                            for (int k = 0; k < NumModels(); k++)
                            {
                                w.WriteStartObject();

                                w.WritePropertyName("model");
                                if (k == 0 && _parent != _name)
                                    w.WriteValue(_name);
                                else
                                    w.WriteValue(_name + "_" + k.ToString());

                                if (i != 0) // Do not need to write if it's 0.
                                {
                                    w.WritePropertyName("x");
                                    w.WriteValue(i * 90);
                                }
                                if (j != 0) // Same as above.
                                {
                                    w.WritePropertyName("y");
                                    w.WriteValue(j * 90);
                                }
                                if (_uvLock)
                                {
                                    w.WritePropertyName("uvlock");
                                    w.WriteValue(true); // If this piece of code is executed, then it must be true.
                                }

                                w.WriteEndObject();
                            }
                        }
        }
        public override List<Model> ConvertToIndividualModels()
        {
            int _modelNo = 0, _groupNo = 0;
            List<Texture> _modelTextures = new List<Texture>();
            List<Model> models = new List<Model>();
            ConvertModelRecur(_modelTextures, 0, ref _modelNo, _groupNo, models);
            return models;
        }
        /// <summary>
        /// Create Model objects for every possible combination
        /// </summary>
        /// <param name="ModelTextures"></param>
        /// <param name="Face"></param>
        /// <param name="modelNo"></param>
        /// <param name="groupNo"></param>
        /// <param name="Models"></param>
        private void ConvertModelRecur(List<Texture> ModelTextures, int Face, ref int modelNo, int groupNo, List<Model> Models)
        {
            if (groupNo < _textureGroups.Count)
            {
                if (Face < _textureGroups[groupNo].Faces.Count)
                {
                    for (int i = 0; i < _textureGroups[groupNo].Textures.Count; i++)
                    {
                        ModelTextures.Add(new Texture(_textureGroups[groupNo].Faces[Face], _textureGroups[groupNo].Textures[i]));
                        if (Face >= _textureGroups[groupNo].Faces.Count - 1)
                            ConvertModelRecur(ModelTextures, 0, ref modelNo, groupNo + 1, Models);
                        else
                            ConvertModelRecur(ModelTextures, Face + 1, ref modelNo, groupNo, Models);
                        ModelTextures.RemoveAt(ModelTextures.Count - 1);
                    }
                }
            }
            else
            {
                // Create a new list based on the current one.
                // Can't use ModelTexures, or it will use the same reference.
                List<Texture> t = new List<Texture>(ModelTextures);
                Models.Add(new Model(_name, _parent, t, _xRot, _yRot)); 
            }
        }
        /// <summary>
        /// Name of the Model
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return _name;
        }
    }
}