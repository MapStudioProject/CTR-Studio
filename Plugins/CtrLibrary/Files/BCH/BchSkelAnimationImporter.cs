using IONET;
using IONET.Core;
using IONET.Core.Animation;
using IONET.Core.Model;
using IONET.Core.Skeleton;
using SPICA.Formats.Common;
using SPICA.Formats.CtrH3D.Animation;
using SPICA.Formats.CtrH3D.Model;
using SPICA.Rendering.SPICA_GL;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;

namespace CtrLibrary
{
    public class BchSkelAnimationImporter
    {
        public class ExportSettings
        {
            public bool ExportMatrices = false;
        }

        public static void Export(H3DAnimation gfxAnimation, H3DModel model, string filePath)
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
                    case H3DPrimitiveType.QuatTransform:
                        ConvertQuatTransform(group, (H3DAnimQuatTransform)element.Content, settings);
                        break;
                    case H3DPrimitiveType.MtxTransform:
                        ConvertMtxTransform(group, (H3DAnimMtxTransform)element.Content, settings);
                        break;
                    case H3DPrimitiveType.Transform:
                        ConvertSRTTransform(group, (H3DAnimTransform)element.Content, parent);
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

        static void ConvertSRTTransform(IOAnimation group, H3DAnimTransform transform, H3DBone parent)
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

        static IOAnimationTrack ConvertTrack(H3DFloatKeyFrameGroup keyFrameGroup, IOAnimationTrackType type)
        {
            IOAnimationTrack track = new IOAnimationTrack();
            track.PreWrap = ConvertLoop(keyFrameGroup.PreRepeat);
            track.PostWrap = ConvertLoop(keyFrameGroup.PostRepeat);
            track.ChannelType = type;

            foreach (var key in keyFrameGroup.KeyFrames)
            {
                if (keyFrameGroup.InterpolationType == H3DInterpolationType.Hermite)
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

        static void ConvertQuatTransform(IOAnimation group, H3DAnimQuatTransform transform, ExportSettings settings)
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

        static void ConvertMtxTransform(IOAnimation group, H3DAnimMtxTransform transform, ExportSettings settings)
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

        public static H3DAnimation Import(string filePath, H3DModel model)
        {
            var gfxAnimation = new H3DAnimation();
            gfxAnimation.Name = Path.GetFileNameWithoutExtension(filePath);
            gfxAnimation.AnimationFlags = H3DAnimationFlags.IsLooping;
            gfxAnimation.MetaData = new SPICA.Formats.CtrH3D.H3DMetaData();

            Import(filePath, gfxAnimation, model);

            return gfxAnimation;
        }

        public static void Import(string filePath, H3DAnimation gfxAnimation, H3DModel model)
        {
            var scene = IOManager.LoadScene(filePath, new ImportSettings());
            var ioanim = scene.Animations.FirstOrDefault();
            if (ioanim == null)
                throw new Exception($"Failed to find animation in file!");

            gfxAnimation.AnimationType = H3DAnimationType.Skeletal;
            gfxAnimation.FramesCount = ioanim.EndFrame != 0 ? ioanim.EndFrame : ioanim.GetFrameCount();
            gfxAnimation.Elements.Clear();

            void LoadGroups(IOAnimation animation)
            {
                foreach (var anim in animation.Groups)
                    LoadGroups(anim);

                //Ignore groups with no tracks
                if (animation.Tracks.Count == 0)
                    return;

                //create a bone anim element
                H3DAnimationElement element = new H3DAnimationElement();
                element.Name = animation.Name;
                gfxAnimation.Elements.Add(element);

                //Normal keyed SRT
                var type = H3DPrimitiveType.Transform;

                if (animation.Tracks.Any(x => x.ChannelType == IOAnimationTrackType.TransformMatrix4x4))
                {
                    type = H3DPrimitiveType.MtxTransform; //baked matrices
                }
                else if (animation.Tracks.Any(x => x.ChannelType == IOAnimationTrackType.QuatX))
                {
                    type = H3DPrimitiveType.QuatTransform; //baked with SRT (quats)
                }

                //Determine the type to use
                element.PrimitiveType = type;
                switch (type)
                {
                    case H3DPrimitiveType.Transform:
                        element.Content = ImportTransform(animation);
                        break;
                    case H3DPrimitiveType.MtxTransform:
                        element.Content = ImportMtxTransform(animation);
                        break;
                    case H3DPrimitiveType.QuatTransform:
                        element.Content = ImportQuatTransform(animation, (int)gfxAnimation.FramesCount);
                        break;
                    default:
                        throw new Exception($"Unsupported primitive type! {type}");
                }
            };

            foreach (var anim in ioanim.Groups)
                LoadGroups(anim);
        }

        private static H3DAnimMtxTransform ImportMtxTransform(IOAnimation animation)
        {
            var track = animation.Tracks.FirstOrDefault(x => x.ChannelType == IOAnimationTrackType.TransformMatrix4x4);
            if (track == null)
                return new H3DAnimMtxTransform();

            var transform = new H3DAnimMtxTransform();
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

        private static H3DAnimQuatTransform ImportQuatTransform(IOAnimation animation, int frameCount)
        {
            var transform = new H3DAnimQuatTransform();

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
            bool hasQuat = quatX != null || quatY != null || quatZ != null;
            bool hasEuler = rotX != null || rotY != null || rotZ != null;

            for (int i = 0; i < frameCount; i++)
            {
                Vector3 position = new Vector3(
                    posX != null ? posX.GetFrameValue(i) : 0,
                    posY != null ? posY.GetFrameValue(i) : 0,
                    posZ != null ? posZ.GetFrameValue(i) : 0);

                Vector3 scale = new Vector3(
                    scaX != null ? scaX.GetFrameValue(i) : 1,
                    scaY != null ? scaY.GetFrameValue(i) : 1,
                    scaZ != null ? scaZ.GetFrameValue(i) : 1);

                Quaternion quat = new Quaternion(
                    quatX != null ? quatX.GetFrameValue(i) : 0,
                    quatY != null ? quatY.GetFrameValue(i) : 0,
                    quatZ != null ? quatZ.GetFrameValue(i) : 0,
                    quatW != null ? quatW.GetFrameValue(i) : 1);

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

        private static H3DAnimTransform ImportTransform(IOAnimation animation)
        {
            var transform = new H3DAnimTransform();

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

        private static void ConvertKeyGroup(H3DFloatKeyFrameGroup group, IOAnimationTrack track)
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
            group.InterpolationType = H3DInterpolationType.Linear;

            bool isLinear = !track.KeyFrames.Any(x => x is IOKeyFrameHermite || x is IOKeyFrameBezier);

            if (!isLinear)
                group.InterpolationType = H3DInterpolationType.Hermite;

            if (isLinear)
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

        static H3DLoopType ConvertLoop(IOCurveWrapMode wrap)
        {
            switch (wrap)
            {
                case IOCurveWrapMode.Linear: return H3DLoopType.Repeat;
                case IOCurveWrapMode.CycleRelative: return H3DLoopType.RelativeRepeat;
                case IOCurveWrapMode.Oscillate: return H3DLoopType.MirroredRepeat; //back and forth
                default:
                    return H3DLoopType.None;
            }
        }

        static IOCurveWrapMode ConvertLoop(H3DLoopType wrap)
        {
            switch (wrap)
            {
                case H3DLoopType.Repeat: return IOCurveWrapMode.Cycle;
                case H3DLoopType.RelativeRepeat: return IOCurveWrapMode.CycleRelative;
                case H3DLoopType.MirroredRepeat: return IOCurveWrapMode.Oscillate; //back and forth
                default:
                    return IOCurveWrapMode.Linear;
            }
        }
    }
}
