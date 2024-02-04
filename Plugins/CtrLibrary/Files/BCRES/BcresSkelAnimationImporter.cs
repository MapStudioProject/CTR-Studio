using CtrLibrary.Bcres;
using IONET;
using IONET.Core;
using IONET.Core.Animation;
using IONET.Core.Model;
using IONET.Core.Skeleton;
using SPICA.Formats.Common;
using SPICA.Formats.CtrGfx;
using SPICA.Formats.CtrGfx.Animation;
using SPICA.Formats.CtrGfx.Model;
using SPICA.Formats.CtrH3D.Animation;
using SPICA.Formats.CtrH3D.Model;
using SPICA.Rendering.SPICA_GL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;
using Toolbox.Core.Animations;
using static GLFrameworkEngine.CameraFrame;
using static SPICA.Rendering.Animation.SkeletalAnimation;

namespace CtrLibrary
{
    public class BcresSkelAnimationImporter
    {
        public class ExportSettings
        {
            public bool ExportMatrices = false;
        }

        public class BcresImportSettings
        {
            public bool BakeAsMatrices = false;
            public bool BakeAsQuat = false;
        }

        public static void Export(GfxAnimation gfxAnimation, H3DModel model, string filePath)
        {
            IOAnimation anm = new IOAnimation();
            anm.Name = Path.GetFileNameWithoutExtension(filePath);
            anm.StartFrame = 0;
            anm.EndFrame = (float)gfxAnimation.FramesCount;

            ExportSettings settings = new ExportSettings();

            foreach (var element in gfxAnimation.Elements)
            {
                IOAnimation group = new IOAnimation();
                group.Name = element.Name;
                anm.Groups.Add(group);

                Vector3 InvScale = Vector3.One;
                H3DBone parent = null;

                if (model != null && model.Skeleton.Contains(element.Name))
                {
                    var bone = model.Skeleton[element.Name];
                    if ((bone.Flags & H3DBoneFlags.IsSegmentScaleCompensate) != 0)
                        parent = bone.ParentIndex != -1 ? model.Skeleton[bone.ParentIndex] : null;
                }

                switch (element.PrimitiveType)
                {
                    case GfxPrimitiveType.QuatTransform:
                        ConvertQuatTransform(group, (GfxAnimQuatTransform)element.Content, settings);
                        break;
                    case GfxPrimitiveType.MtxTransform:
                        ConvertMtxTransform(group, (GfxAnimMtxTransform)element.Content, settings);
                        break;
                    case GfxPrimitiveType.Transform:
                        ConvertSRTTransform(group, (GfxAnimTransform)element.Content, parent);
                        break;
                    default:
                        throw new Exception($"Unsupported primitive type! {element.PrimitiveType}");
                }
            }

            IOModel iomodel = new IOModel();
            iomodel.Name = model.Name;


            //Convert skeleton
            List<IOBone> bones = new List<IOBone>();
            foreach (var bone in model.Skeleton)
            {
                IOBone iobone = new IOBone();
                iobone.Name = bone.Name;
                iobone.Scale = new Vector3(bone.Scale.X, bone.Scale.Y, bone.Scale.Z);
                iobone.RotationEuler = new Vector3(bone.Rotation.X, bone.Rotation.Y, bone.Rotation.Z);
                iobone.Translation = new Vector3(bone.Translation.X, bone.Translation.Y, bone.Translation.Z);
                bones.Add(iobone);
            }
            //setup children and root
            for (int i = 0; i < bones.Count; i++)
            {
                var bone = bones[i];
                var parentIdx = model.Skeleton[i].ParentIndex;
                if (parentIdx == -1)
                    iomodel.Skeleton.RootBones.Add(bone);
                else
                    bones[parentIdx].AddChild(bone);
            }

            IOScene scene = new IOScene();
            scene.Animations.Add(anm);
            scene.Models.Add(iomodel);
            scene.Name = anm.Name;

            IOManager.ExportScene(scene, filePath);
        }

        static void ConvertSRTTransform(IOAnimation group, GfxAnimTransform transform, H3DBone parent)
        {
            //SRT keyed
            if (transform.ScaleExists)
            {
                if (transform.ScaleX.KeyFrames.Count > 0)
                    group.Tracks.Add(ConvertTrack(transform.ScaleX, IOAnimationTrackType.ScaleX));
                if (transform.ScaleY.KeyFrames.Count > 0)
                    group.Tracks.Add(ConvertTrack(transform.ScaleY, IOAnimationTrackType.ScaleY));
                if (transform.ScaleZ.KeyFrames.Count > 0)
                    group.Tracks.Add(ConvertTrack(transform.ScaleZ, IOAnimationTrackType.ScaleZ));
            }
            if (transform.RotationExists)
            {
                if (transform.RotationX.KeyFrames.Count > 0)
                    group.Tracks.Add(ConvertTrack(transform.RotationX, IOAnimationTrackType.RotationEulerX));
                if (transform.RotationY.KeyFrames.Count > 0)
                    group.Tracks.Add(ConvertTrack(transform.RotationY, IOAnimationTrackType.RotationEulerY));
                if (transform.RotationZ.KeyFrames.Count > 0)
                    group.Tracks.Add(ConvertTrack(transform.RotationZ, IOAnimationTrackType.RotationEulerZ));
            }
            if (transform.TranslationExists)
            {
                if (transform.TranslationX.KeyFrames.Count > 0)
                    group.Tracks.Add(ConvertTrack(transform.TranslationX, IOAnimationTrackType.PositionX));
                if (transform.TranslationY.KeyFrames.Count > 0)
                    group.Tracks.Add(ConvertTrack(transform.TranslationY, IOAnimationTrackType.PositionY));
                if (transform.TranslationZ.KeyFrames.Count > 0)
                    group.Tracks.Add(ConvertTrack(transform.TranslationZ, IOAnimationTrackType.PositionZ));
            }
        }

        static IOAnimationTrack ConvertTrack(GfxFloatKeyFrameGroup keyFrameGroup, IOAnimationTrackType type)
        {
            IOAnimationTrack track = new IOAnimationTrack();
            track.PreWrap = ConvertLoop(keyFrameGroup.PreRepeat);
            track.PostWrap = ConvertLoop(keyFrameGroup.PostRepeat);
            track.ChannelType = type;

            foreach (var key in keyFrameGroup.KeyFrames)
            {
                if (!keyFrameGroup.IsLinear)
                {
                    track.KeyFrames.Add(new IOKeyFrameHermite()
                    {
                        Frame = key.Frame,
                        Value = key.Value,
                        TangentSlopeInput = key.InSlope,
                        TangentSlopeOutput = key.OutSlope,
                    });
                }
                else
                {
                    track.KeyFrames.Add(new IOKeyFrame()
                    {
                        Frame = key.Frame,
                        Value = key.Value,
                    });
                }
            }
            return track;
        }

        static void ConvertQuatTransform(IOAnimation group, GfxAnimQuatTransform transform, ExportSettings settings)
        {
            //baked with quats
            if (settings.ExportMatrices) //convert data to matrices
            {
                int count = transform.HasTranslation ? transform.Translations.Count : 0;
                if (transform.HasRotation)
                    count = transform.Rotations.Count;
                if (transform.HasScale)
                    count = transform.Scales.Count;

                IOAnimationTrack track = new IOAnimationTrack() { ChannelType = IOAnimationTrackType.TransformMatrix4x4 };
                group.Tracks.Add(track);

                for (int i = 0; i < count; i++)
                {
                    var quat = transform.HasRotation ? transform.Rotations[i] : Quaternion.Identity;
                    var sca = transform.HasScale ? transform.Scales[i] : Vector3.One;
                    var pos = transform.HasTranslation ? transform.Translations[i] : Vector3.Zero;

                    var mtxScale = OpenTK.Matrix4.CreateScale(sca.X, sca.Y, sca.Z);
                    var mtxRot = OpenTK.Matrix4.CreateFromQuaternion(new OpenTK.Quaternion(quat.X, quat.Y, quat.Z, quat.W));
                    var mtxTrans = OpenTK.Matrix4.CreateTranslation(pos.X, pos.Y, pos.Z);
                    var matrix = mtxScale * mtxRot * mtxTrans;

                    track.KeyFrames.Add(new IOKeyFrame()
                    {
                        Frame = i,
                        Value = new float[16]
                        {
                            matrix.M11, matrix.M21, matrix.M31, matrix.M41,
                            matrix.M12, matrix.M22, matrix.M32, matrix.M42,
                            matrix.M13, matrix.M23, matrix.M33, matrix.M43,
                            0, 0, 0, 1,
                        }
                    });
                }
            }
            else //else key the data and convert quat to euler. Don't use quat directly as most formats do not support them
            {
                if (transform.HasTranslation)
                {
                    IOAnimationTrack x = new IOAnimationTrack() { ChannelType = IOAnimationTrackType.PositionX };
                    IOAnimationTrack y = new IOAnimationTrack() { ChannelType = IOAnimationTrackType.PositionX };
                    IOAnimationTrack z = new IOAnimationTrack() { ChannelType = IOAnimationTrackType.PositionX };
                    group.Tracks.Add(x);
                    group.Tracks.Add(y);
                    group.Tracks.Add(z);

                    for (int i = 0; i < transform.Translations.Count; i++)
                    {
                        x.KeyFrames.Add(new IOKeyFrame() { Frame = i, Value = transform.Translations[i].X });
                        y.KeyFrames.Add(new IOKeyFrame() { Frame = i, Value = transform.Translations[i].Y });
                        z.KeyFrames.Add(new IOKeyFrame() { Frame = i, Value = transform.Translations[i].Z });
                    }
                }
                if (transform.HasRotation)
                {
                    IOAnimationTrack x = new IOAnimationTrack() { ChannelType = IOAnimationTrackType.RotationEulerX };
                    IOAnimationTrack y = new IOAnimationTrack() { ChannelType = IOAnimationTrackType.RotationEulerY };
                    IOAnimationTrack z = new IOAnimationTrack() { ChannelType = IOAnimationTrackType.RotationEulerZ };
                    group.Tracks.Add(x);
                    group.Tracks.Add(y);
                    group.Tracks.Add(z);

                    for (int i = 0; i < transform.Rotations.Count; i++)
                    {
                        var rot = ToEuler(transform.Rotations[i]);

                        x.KeyFrames.Add(new IOKeyFrame() { Frame = i, Value = rot.X });
                        y.KeyFrames.Add(new IOKeyFrame() { Frame = i, Value = rot.Y });
                        z.KeyFrames.Add(new IOKeyFrame() { Frame = i, Value = rot.Z });
                    }
                }
                if (transform.HasScale)
                {
                    IOAnimationTrack x = new IOAnimationTrack() { ChannelType = IOAnimationTrackType.ScaleX };
                    IOAnimationTrack y = new IOAnimationTrack() { ChannelType = IOAnimationTrackType.ScaleY };
                    IOAnimationTrack z = new IOAnimationTrack() { ChannelType = IOAnimationTrackType.ScaleZ };
                    group.Tracks.Add(x);
                    group.Tracks.Add(y);
                    group.Tracks.Add(z);

                    for (int i = 0; i < transform.Scales.Count; i++)
                    {
                        x.KeyFrames.Add(new IOKeyFrame() { Frame = i, Value = transform.Scales[i].X });
                        y.KeyFrames.Add(new IOKeyFrame() { Frame = i, Value = transform.Scales[i].Y });
                        z.KeyFrames.Add(new IOKeyFrame() { Frame = i, Value = transform.Scales[i].Z });
                    }
                }
            }
        }

        static Vector3 ToEuler(OpenTK.Quaternion q) => ToEuler(new System.Numerics.Quaternion(q.X, q.Y, q.Z, q.W));

        static Vector3 ToEuler(Quaternion q)
        {
            return new Vector3(
                (float)Math.Atan2(2 * (q.X * q.W + q.Y * q.Z), 1 - 2 * (q.X * q.X + q.Y * q.Y)),
                -(float)Math.Asin(2 * (q.X * q.Z - q.W * q.Y)),
                (float)Math.Atan2(2 * (q.X * q.Y + q.Z * q.W), 1 - 2 * (q.Y * q.Y + q.Z * q.Z)));
        }

        static void ConvertMtxTransform(IOAnimation group, GfxAnimMtxTransform transform, ExportSettings settings)
        {
            //baked matrices
            if (settings.ExportMatrices)
            {
                IOAnimationTrack track = new IOAnimationTrack() { ChannelType = IOAnimationTrackType.TransformMatrix4x4 };
                group.Tracks.Add(track);

                for (int i = 0; i < transform.Frames.Count; i++)
                {
                    var matrix = transform.Frames[i];
                    track.KeyFrames.Add(new IOKeyFrame()
                    {
                        Frame = i,
                        Value = new float[16] 
                        {
                            matrix.M11, matrix.M21, matrix.M31, matrix.M41,
                            matrix.M12, matrix.M22, matrix.M32, matrix.M42,
                            matrix.M13, matrix.M23, matrix.M33, matrix.M43,
                            0, 0, 0, 1,
                        }
                    });
                }
            }
            else //compose data from the matrices
            {
                IOAnimationTrack px = new IOAnimationTrack() { ChannelType = IOAnimationTrackType.PositionX };
                IOAnimationTrack py = new IOAnimationTrack() { ChannelType = IOAnimationTrackType.PositionY };
                IOAnimationTrack pz = new IOAnimationTrack() { ChannelType = IOAnimationTrackType.PositionZ };
                IOAnimationTrack rx = new IOAnimationTrack() { ChannelType = IOAnimationTrackType.RotationEulerX };
                IOAnimationTrack ry = new IOAnimationTrack() { ChannelType = IOAnimationTrackType.RotationEulerY };
                IOAnimationTrack rz = new IOAnimationTrack() { ChannelType = IOAnimationTrackType.RotationEulerZ };
                IOAnimationTrack sx = new IOAnimationTrack() { ChannelType = IOAnimationTrackType.ScaleX };
                IOAnimationTrack sy = new IOAnimationTrack() { ChannelType = IOAnimationTrackType.ScaleY };
                IOAnimationTrack sz = new IOAnimationTrack() { ChannelType = IOAnimationTrackType.ScaleZ };

                group.Tracks.Add(px);
                group.Tracks.Add(py);
                group.Tracks.Add(pz);
                group.Tracks.Add(rx);
                group.Tracks.Add(ry);
                group.Tracks.Add(rz);
                group.Tracks.Add(sx);
                group.Tracks.Add(sy);
                group.Tracks.Add(sz);

                for (int i = 0; i < transform.Frames.Count; i++)
                {
                    var matrix = transform.Frames[i];
                    var mat4 = matrix.ToMatrix4x4().ToMatrix4();

                    var rot = ToEuler(mat4.ExtractRotation());
                    var sca = mat4.ExtractScale();
                    var pos = mat4.ExtractTranslation();

                    px.KeyFrames.Add(new IOKeyFrame() { Frame = i, Value = pos.X });
                    py.KeyFrames.Add(new IOKeyFrame() { Frame = i, Value = pos.Y });
                    pz.KeyFrames.Add(new IOKeyFrame() { Frame = i, Value = pos.Z });
                    sx.KeyFrames.Add(new IOKeyFrame() { Frame = i, Value = sca.X });
                    sy.KeyFrames.Add(new IOKeyFrame() { Frame = i, Value = sca.Y });
                    sz.KeyFrames.Add(new IOKeyFrame() { Frame = i, Value = sca.Z });
                    rx.KeyFrames.Add(new IOKeyFrame() { Frame = i, Value = rot.X });
                    ry.KeyFrames.Add(new IOKeyFrame() { Frame = i, Value = rot.Y });
                    rz.KeyFrames.Add(new IOKeyFrame() { Frame = i, Value = rot.Z });
                }
            }
        }

        public static GfxAnimation Import(string filePath, H3DModel model, BcresImportSettings settings = null)
        {
            var gfxAnimation = new GfxAnimation();
            gfxAnimation.Name = Path.GetFileNameWithoutExtension(filePath);
            gfxAnimation.LoopMode = GfxLoopMode.Loop;
            gfxAnimation.MetaData = new GfxDict<GfxMetaData>();

            Import(filePath, gfxAnimation, model, settings);

            return gfxAnimation;
        }

        public static void Import(string filePath, GfxAnimation gfxAnimation, H3DModel model, BcresImportSettings settings = null)
        {
            if (settings == null) settings = new BcresImportSettings();

            var scene = IOManager.LoadScene(filePath, new ImportSettings());
            var ioanim = scene.Animations.FirstOrDefault();
            if (ioanim == null)
                throw new Exception($"Failed to find animation in file!");

            gfxAnimation.TargetAnimGroupName = "SkeletalAnimation";
            gfxAnimation.FramesCount = ioanim.EndFrame != 0 ? ioanim.EndFrame : ioanim.GetFrameCount();
            gfxAnimation.Elements.Clear();

            Dictionary<string, Matrix4x4[]> baked_matrices = new Dictionary<string, Matrix4x4[]>();
            if (settings.BakeAsMatrices)
            {
                baked_matrices = BakeMatrices(ioanim, model, gfxAnimation.FramesCount);

                foreach (var bone in baked_matrices)
                {
                    GfxAnimationElement element = new GfxAnimationElement();
                    element.Name = bone.Key;
                    element.PrimitiveType = GfxPrimitiveType.MtxTransform;
                    gfxAnimation.Elements.Add(element);

                    var transform = new GfxAnimMtxTransform();
                    transform.StartFrame = 0;
                    transform.EndFrame = gfxAnimation.FramesCount;
                    element.Content = transform;

                    foreach (var mat in bone.Value)
                        transform.Frames.Add(new SPICA.Math3D.Matrix3x4(mat));
                }

                return;
            }

            void LoadGroups(IOAnimation animation)
            {
                foreach (var anim in animation.Groups)
                    LoadGroups(anim);

                //Ignore groups with no tracks
                if (animation.Tracks.Count == 0)
                    return;

                //Check if group is already loaded
                if (gfxAnimation.Elements.Any(x => x.Name == animation.Name))
                    return;

                //create a bone anim element
                GfxAnimationElement element = new GfxAnimationElement();
                element.Name = animation.Name;
                gfxAnimation.Elements.Add(element);

                //Normal keyed SRT
                var type = GfxPrimitiveType.Transform;

                if (animation.Tracks.Any(x => x.ChannelType == IOAnimationTrackType.TransformMatrix4x4 || settings.BakeAsMatrices))
                {
                    type = GfxPrimitiveType.MtxTransform; //baked matrices
                }
                else if (animation.Tracks.Any(x => x.ChannelType == IOAnimationTrackType.QuatX) || settings.BakeAsQuat)
                {
                    type = GfxPrimitiveType.QuatTransform; //baked with SRT (quats)
                }

                //Determine the type to use
                element.PrimitiveType = type;
                switch (type)
                {
                    case GfxPrimitiveType.Transform:
                        element.Content = ImportTransform(animation);
                        break;
                    case GfxPrimitiveType.MtxTransform:
                        element.Content = ImportMtxTransform(animation, model);
                        break;
                    case GfxPrimitiveType.QuatTransform:
                        element.Content = ImportQuatTransform(animation, model,(int)gfxAnimation.FramesCount);
                        break;
                    default:
                        throw new Exception($"Unsupported primitive type! {type}");
                }
            };

            foreach (var anim in ioanim.Groups)
                LoadGroups(anim);
        }

        private static Dictionary<string, Matrix4x4[]> BakeMatrices(IOAnimation ioanim, H3DModel model, float count)
        {
            Dictionary<string, Matrix4x4[]> baked_matrices = new Dictionary<string, Matrix4x4[]>();

            int frameCount = (int)count;

            void LoadGroups(IOAnimation animation)
            {
                foreach (var anim in animation.Groups)
                    LoadGroups(anim);

                //Ignore groups with no tracks
                if (animation.Tracks.Count == 0)
                    return;

                var bone = model.Skeleton[animation.Name];

                //decompose into mtx transform
                var posX = animation.Tracks.FirstOrDefault(x => x.ChannelType == IOAnimationTrackType.PositionX);
                var posY = animation.Tracks.FirstOrDefault(x => x.ChannelType == IOAnimationTrackType.PositionY);
                var posZ = animation.Tracks.FirstOrDefault(x => x.ChannelType == IOAnimationTrackType.PositionZ);

                var scaX = animation.Tracks.FirstOrDefault(x => x.ChannelType == IOAnimationTrackType.ScaleX);
                var scaY = animation.Tracks.FirstOrDefault(x => x.ChannelType == IOAnimationTrackType.ScaleY);
                var scaZ = animation.Tracks.FirstOrDefault(x => x.ChannelType == IOAnimationTrackType.ScaleZ);

                var rotX = animation.Tracks.FirstOrDefault(x => x.ChannelType == IOAnimationTrackType.RotationEulerX);
                var rotY = animation.Tracks.FirstOrDefault(x => x.ChannelType == IOAnimationTrackType.RotationEulerY);
                var rotZ = animation.Tracks.FirstOrDefault(x => x.ChannelType == IOAnimationTrackType.RotationEulerZ);

                baked_matrices.Add(animation.Name, new Matrix4x4[frameCount]);
                for (int i = 0; i < frameCount; i++)
                {
                    var position = new Vector3(
                            posX != null ? posX.GetFrameValue(i) : bone.Translation.X,
                            posY != null ? posY.GetFrameValue(i) : bone.Translation.Y,
                            posZ != null ? posZ.GetFrameValue(i) : bone.Translation.Z);
                    var scale = new Vector3(
                    scaX != null ? scaX.GetFrameValue(i) : bone.Scale.X,
                        scaY != null ? scaY.GetFrameValue(i) : bone.Scale.Y,
                        scaZ != null ? scaZ.GetFrameValue(i) : bone.Scale.Z);
                    var rot = new Vector3(
                               rotX != null ? rotX.GetFrameValue(i) : bone.Rotation.X,
                               rotY != null ? rotY.GetFrameValue(i) : bone.Rotation.Y,
                               rotZ != null ? rotZ.GetFrameValue(i) : bone.Rotation.Z);

                    var mat = Matrix4x4.CreateScale(scale) *
                        (Matrix4x4.CreateRotationX(rot.X) *
                         Matrix4x4.CreateRotationY(rot.Y) *
                         Matrix4x4.CreateRotationZ(rot.Z)) *
                        Matrix4x4.CreateTranslation(position);
                    baked_matrices[animation.Name][i] = mat;
                }
            }

            foreach (var anim in ioanim.Groups)
                LoadGroups(anim);

            Matrix4x4 GetParentMatrix(H3DBone bone, int frame)
            {
                if (baked_matrices.ContainsKey(bone.Name))
                    return baked_matrices[bone.Name][frame];

                return bone.Transform;
            }

            //lastly update parents
            foreach (var bone in model.Skeleton)
            {
                if (baked_matrices.ContainsKey(bone.Name))
                {
                    if (bone.ParentIndex != -1)
                    {
                        for (int i = 0; i < baked_matrices[bone.Name].Length; i++)
                            baked_matrices[bone.Name][i] *= GetParentMatrix(model.Skeleton[bone.ParentIndex], i);
                    }
                }
            }

            return baked_matrices;
        }

        private static GfxAnimMtxTransform ImportMtxTransform(IOAnimation animation, H3DModel model)
        {
            var track = animation.Tracks.FirstOrDefault(x => x.ChannelType == IOAnimationTrackType.TransformMatrix4x4);
            if (track == null)
            {
                return new GfxAnimMtxTransform();
            }
            else //Else produce using a key framed transform matrix4x4
            {
                var transform = new GfxAnimMtxTransform();
                transform.StartFrame = 0;
                transform.EndFrame = track.KeyFrames.LastOrDefault().Frame;
                transform.PreRepeat = ConvertLoop(track.PreWrap);
                transform.PostRepeat = ConvertLoop(track.PreWrap);

                foreach (var key in track.KeyFrames)
                {
                    float[] mat = (float[])key.Value;
                    transform.Frames.Add(new SPICA.Math3D.Matrix3x4(
                        mat[0], mat[1], mat[2], mat[3],
                        mat[4], mat[5], mat[6], mat[7],
                        mat[8], mat[9], mat[10], mat[11]));
                }
                return transform;
            }
        }

        private static GfxAnimQuatTransform ImportQuatTransform(IOAnimation animation, H3DModel model, int frameCount)
        {
            var transform = new GfxAnimQuatTransform();

            var bone = model.Skeleton.Contains(animation.Name) ? model.Skeleton[animation.Name] : new H3DBone();
            var bone_quat = Quaternion.CreateFromYawPitchRoll(bone.Rotation.X, bone.Rotation.Y, bone.Rotation.Z);

            var posX = animation.Tracks.FirstOrDefault(x => x.ChannelType == IOAnimationTrackType.PositionX);
            var posY = animation.Tracks.FirstOrDefault(x => x.ChannelType == IOAnimationTrackType.PositionY);
            var posZ = animation.Tracks.FirstOrDefault(x => x.ChannelType == IOAnimationTrackType.PositionZ);

            var scaX = animation.Tracks.FirstOrDefault(x => x.ChannelType == IOAnimationTrackType.ScaleX);
            var scaY = animation.Tracks.FirstOrDefault(x => x.ChannelType == IOAnimationTrackType.ScaleY);
            var scaZ = animation.Tracks.FirstOrDefault(x => x.ChannelType == IOAnimationTrackType.ScaleZ);

            var quatX = animation.Tracks.FirstOrDefault(x => x.ChannelType == IOAnimationTrackType.QuatX);
            var quatY = animation.Tracks.FirstOrDefault(x => x.ChannelType == IOAnimationTrackType.QuatY);
            var quatZ = animation.Tracks.FirstOrDefault(x => x.ChannelType == IOAnimationTrackType.QuatZ);
            var quatW = animation.Tracks.FirstOrDefault(x => x.ChannelType == IOAnimationTrackType.QuatW);

            var rotX = animation.Tracks.FirstOrDefault(x => x.ChannelType == IOAnimationTrackType.RotationEulerX);
            var rotY = animation.Tracks.FirstOrDefault(x => x.ChannelType == IOAnimationTrackType.RotationEulerY);
            var rotZ = animation.Tracks.FirstOrDefault(x => x.ChannelType == IOAnimationTrackType.RotationEulerZ);

            bool hasScale = scaX != null || scaY != null || scaZ != null;
            bool hasPos = posX != null || posY != null || posZ != null;
            bool hasQuat = quatX != null || quatY != null || quatZ != null || quatW != null;
            bool hasEuler = rotX != null || rotY != null || rotZ != null;

            for (int i = 0; i < frameCount; i++)
            {
                Vector3 position = new Vector3(
                    posX != null ? posX.GetFrameValue(i) : bone.Translation.X,
                    posY != null ? posY.GetFrameValue(i) : bone.Translation.Y,
                    posZ != null ? posZ.GetFrameValue(i) : bone.Translation.Z);

                Vector3 scale = new Vector3(
                    scaX != null ? scaX.GetFrameValue(i) : bone.Scale.X,
                    scaY != null ? scaY.GetFrameValue(i) : bone.Scale.Y,
                    scaZ != null ? scaZ.GetFrameValue(i) : bone.Scale.Z);

                Quaternion quat = new Quaternion(
                    quatX != null ? quatX.GetFrameValue(i) : bone_quat.X,
                    quatY != null ? quatY.GetFrameValue(i) : bone_quat.Y,
                    quatZ != null ? quatZ.GetFrameValue(i) : bone_quat.Z,
                    quatW != null ? quatW.GetFrameValue(i) : bone_quat.W);

                if (hasPos)
                    transform.Translations.Add(position);

                if (hasScale)
                    transform.Scales.Add(scale);

                if (hasQuat)
                {
                    transform.Rotations.Add(quat);
                }
                else if (hasEuler)
                {
                    Vector3 rot = new Vector3(
                        rotX != null ? rotX.GetFrameValue(i) : 0,
                        rotY != null ? rotY.GetFrameValue(i) : 0,
                        rotZ != null ? rotZ.GetFrameValue(i) : 0);
                    var q = STMath.FromEulerAngles(new OpenTK.Vector3(rot.X, rot.Y, rot.Z));
                    transform.Rotations.Add(new Quaternion(q.X, q.Y, q.Z, q.W));
                }
            }

            return transform;
        }

        private static GfxAnimTransform ImportTransform(IOAnimation animation)
        {
            var transform = new GfxAnimTransform();

            foreach (var track in animation.Tracks)
            {
                switch (track.ChannelType)
                {
                    case IOAnimationTrackType.RotationEulerX: ConvertKeyGroup(transform.RotationX, track); break;
                    case IOAnimationTrackType.RotationEulerY: ConvertKeyGroup(transform.RotationY, track); break;
                    case IOAnimationTrackType.RotationEulerZ: ConvertKeyGroup(transform.RotationZ, track); break;
                    case IOAnimationTrackType.ScaleX: ConvertKeyGroup(transform.ScaleX, track); break;
                    case IOAnimationTrackType.ScaleY: ConvertKeyGroup(transform.ScaleY, track); break;
                    case IOAnimationTrackType.ScaleZ: ConvertKeyGroup(transform.ScaleZ, track); break;
                    case IOAnimationTrackType.PositionX: ConvertKeyGroup(transform.TranslationX, track); break;
                    case IOAnimationTrackType.PositionY: ConvertKeyGroup(transform.TranslationY, track); break;
                    case IOAnimationTrackType.PositionZ: ConvertKeyGroup(transform.TranslationZ, track); break;
                }
            }

            return transform;
        }

        private static void ConvertKeyGroup(GfxFloatKeyFrameGroup group, IOAnimationTrack track)
        {
            bool hasSlopeOut = false;

            foreach (var keyFrame in track.KeyFrames)
            {
                if (keyFrame is IOKeyFrameHermite)
                {
                    var slopeIn = ((IOKeyFrameHermite)keyFrame).TangentSlopeInput;
                    var slopeOut = ((IOKeyFrameHermite)keyFrame).TangentSlopeInput;
                    //Check if slope output is used at all and is different than input slope
                    hasSlopeOut = slopeIn != slopeOut && slopeOut != 0;

                    group.KeyFrames.Add(new KeyFrame()
                    {
                        Frame = keyFrame.Frame,
                        Value = keyFrame.ValueF32,
                        InSlope = slopeIn,
                        OutSlope = slopeOut,
                    });
                }
                else
                {
                    group.KeyFrames.Add(new KeyFrame()
                    {
                        Frame = keyFrame.Frame,
                        Value = keyFrame.ValueF32,
                    });
                }
            }
            group.StartFrame = 0;
            group.EndFrame = group.KeyFrames.LastOrDefault().Frame;
            group.PreRepeat = ConvertLoop(track.PreWrap);
            group.PostRepeat = ConvertLoop(track.PreWrap);
            group.IsLinear = !track.KeyFrames.Any(x => x is IOKeyFrameHermite || x is IOKeyFrameBezier);

            if (group.IsLinear)
            {
                //Float frame and key data
                group.Quantization = KeyFrameQuantization.StepLinear64;
            }
            else
            {
                //Float frame and key data
                group.Quantization = KeyFrameQuantization.Hermite128;
                //No out slopes present, use just key frame, value and single slope
                if (!hasSlopeOut)
                    group.Quantization = KeyFrameQuantization.UnifiedHermite96;
            }
        }

        static GfxLoopType ConvertLoop(IOCurveWrapMode wrap)
        {
            switch (wrap)
            {
                case IOCurveWrapMode.Linear: return GfxLoopType.Repeat;
                case IOCurveWrapMode.CycleRelative: return GfxLoopType.RelativeRepeat;
                case IOCurveWrapMode.Oscillate: return GfxLoopType.MirroredRepeat; //back and forth
                default:
                    return GfxLoopType.None;
            }
        }

        static IOCurveWrapMode ConvertLoop(GfxLoopType wrap)
        {
            switch (wrap)
            {
                case GfxLoopType.Repeat: return IOCurveWrapMode.Cycle;
                case GfxLoopType.RelativeRepeat: return IOCurveWrapMode.CycleRelative;
                case GfxLoopType.MirroredRepeat: return IOCurveWrapMode.Oscillate; //back and forth
                default:
                    return IOCurveWrapMode.Linear;
            }
        }
    }
}
