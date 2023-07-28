using CtrLibrary.Bcres;
using IONET;
using IONET.Core.Animation;
using SPICA.Formats.Common;
using SPICA.Formats.CtrGfx;
using SPICA.Formats.CtrGfx.Animation;
using SPICA.Formats.CtrGfx.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core;

namespace CtrLibrary
{
    public class BcresSkelAnimationImporter
    {
        public static GfxAnimation Import(string filePath)
        {
            var gfxAnimation = new GfxAnimation();
            gfxAnimation.Name = Path.GetFileNameWithoutExtension(filePath);
            gfxAnimation.LoopMode = GfxLoopMode.Loop;
            gfxAnimation.MetaData = new GfxDict<GfxMetaData>();

            Import(filePath, gfxAnimation);

            return gfxAnimation;
        }

        public static void Import(string filePath, GfxAnimation gfxAnimation)
        {
            var scene = IOManager.LoadScene(filePath, new ImportSettings());
            var ioanim = scene.Animations.FirstOrDefault();
            if (ioanim == null)
                throw new Exception($"Failed to find animation in file!");

            gfxAnimation.TargetAnimGroupName = "SkeletalAnimation";
            gfxAnimation.FramesCount = ioanim.EndFrame != 0 ? ioanim.EndFrame : ioanim.GetFrameCount();

            void LoadGroups(IOAnimation animation)
            {
                foreach (var anim in animation.Groups)
                    LoadGroups(anim);

                //Ignore groups with no tracks
                if (animation.Tracks.Count == 0)
                    return;

                //create a bone anim element
                GfxAnimationElement element = new GfxAnimationElement();
                element.Name = animation.Name;

                //Normal keyed SRT
                var type = GfxPrimitiveType.Transform;

                if (animation.Tracks.Any(x => x.ChannelType == IOAnimationTrackType.TransformMatrix4x4))
                {
                    type = GfxPrimitiveType.MtxTransform; //baked matrices
                }
                else if (animation.Tracks.Any(x => x.ChannelType == IOAnimationTrackType.QuatX))
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
                        element.Content = ImportMtxTransform(animation);
                        break;
                    case GfxPrimitiveType.QuatTransform:
                        element.Content = ImportQuatTransform(animation, (int)gfxAnimation.FramesCount);
                        break;
                    default:
                        throw new Exception($"Unsupported primitive type! {type}");
                }
            };

            foreach (var anim in ioanim.Groups)
                LoadGroups(anim);
        }

        private static GfxAnimMtxTransform ImportMtxTransform(IOAnimation animation)
        {
            var track = animation.Tracks.FirstOrDefault(x => x.ChannelType == IOAnimationTrackType.TransformMatrix4x4);
            if (track == null)
                return new GfxAnimMtxTransform();

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

        private static GfxAnimQuatTransform ImportQuatTransform(IOAnimation animation, int frameCount)
        {
            var transform = new GfxAnimQuatTransform();

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
    }
}
