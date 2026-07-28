using UnityEngine;
using DG.Tweening;

namespace DuongMike
{
    public class EffectDotweenDuong
    {
        private float Scale(float duration, float speed) => duration * speed;
        /// <summary>
        /// Set up ban dau cho scale nap lon hon 1 nhe, duration = 0.5f
        /// </summary>
        /// <param name="targetY">Move local to target</param>
        /// <param name="sp">Spriterender</param>
        /// <param name="nameSound">am thanh thuc hien anim</param>
        /// <param name="speed">toc do anim</param>
        /// <returns></returns>
        public Tween ItemAppearLight(float targetY, SpriteRenderer sp, string nameSound = "AppearCartoon", float speed = 1)
        {
            Transform parent = sp.transform.parent.parent;
            //SoundControlDuong.Instance.PlayFX(nameSound);
            Tween tween = DOTween.Sequence()
                  .Append(parent.DOLocalMoveY(targetY, Scale(0.5f, speed)).SetEase(Ease.InQuad))
                .Join(sp.DOFade(1, Scale(0.4f, speed)))
                .Join(parent.DOScale(1, Scale(0.5f, speed)));
            return tween;
        }
        /// <summary>
        /// Set up ban dau cho scale nap lon hon 1 nhe, duration = 0.5
        /// </summary>
        /// <param name="targetY">Move local to target</param>
        /// <param name="sp">Spriterender</param>
        /// <param name="nameSound">am thanh thuc hien anim</param>
        /// <param name="speed">toc do anim</param>
        /// <returns></returns>
        public Tween ItemHideLight(float targetY, SpriteRenderer sp, string nameSound = "AppearCartoon", float speed = 1)
        {
            Transform parent = sp.transform.parent.parent;
            //SoundControlDuong.Instance.PlayFX(nameSound);
            Tween tween = DOTween.Sequence()
                 .Append(parent.DOLocalMoveY(targetY, Scale(0.5f, speed))).SetEase(Ease.OutQuad)
                 .Join(sp.DOFade(0, Scale(0.7f, speed)));
            return tween;
        }
        /// <summary>
        ///  Tween Lat chao
        /// </summary>
        /// <param name="targetTranf"></param>
        public void PlayAnimThrowPan(Transform targetTranf)
        {
            float flipAngle = 35f;
            float moveUp = 0.22f;
            float duration = 0.12f;
            Vector3 startPos = targetTranf.localPosition;
            Vector3 upPos = startPos + new Vector3(0, moveUp, 0);
            Vector3 reboundPos = startPos + new Vector3(0, moveUp * 0.25f, 0);
            var flipSeq = DOTween.Sequence();
            flipSeq.Append(targetTranf.DOLocalMove(upPos, duration).SetEase(Ease.OutSine))
                   .Join(targetTranf.DOLocalRotate(new Vector3(0, 0, flipAngle), duration).SetEase(Ease.OutSine))

                   .Append(targetTranf.DOLocalMove(startPos, duration * 0.9f).SetEase(Ease.InQuad))
                   .Join(targetTranf.DOLocalRotate(Vector3.zero, duration * 0.9f).SetEase(Ease.InQuad))

                   .Append(targetTranf.DOLocalMove(reboundPos, 0.08f).SetEase(Ease.OutQuad))
                   .Append(targetTranf.DOLocalMove(startPos, 0.08f).SetEase(Ease.InQuad));
        }

        public Tween Popup(Transform target, float duration = 0.3f, float startScale = 0.5f, float endScale = 1f)
        {
            target.localScale = Vector3.one * startScale;

            return target.DOScale(endScale, duration)
                .SetEase(Ease.OutBack);
        }

        /// <summary>
        /// Plays popup scale animation while preserving non-uniform target scale values.
        /// </summary>
        public Tween Popup(Transform target, float duration, Vector3 startScale, Vector3 endScale)
        {
            target.localScale = startScale;

            return target.DOScale(endScale, duration)
                .SetEase(Ease.OutBack);
        }

        public Tween PopOut(Transform target, float duration = 0.2f, float endScale = 0f)
        {
            return target.DOScale(endScale, duration)
                .SetEase(Ease.InBack); 
        }

        /// <summary>
        /// Plays pop-out scale animation to a custom non-uniform target scale.
        /// </summary>
        public Tween PopOut(Transform target, float duration, Vector3 endScale)
        {
            return target.DOScale(endScale, duration)
                .SetEase(Ease.InBack);
        }

        public  Tween PunchScale(Transform target, float punch = 0.3f, float duration = 0.3f)
        {
            return target.DOPunchScale(Vector3.one * punch, duration, 10, 1);
        }

       
        public  Tween ShakePosition(Transform target, float duration = 0.3f, float strength = 0.5f)
        {
            return target.DOShakePosition(duration, strength, 20, 90, false, true);
        }

    
        public  Tween ShakeRotation(Transform target, float duration = 0.3f, float strength = 10f)
        {
            return target.DOShakeRotation(duration, strength, 20, 90, true);
        }

      
        public  Tween MoveTo(Transform target, Vector3 endPos, float duration = 0.5f)
        {
            return target.DOMove(endPos, duration)
                .SetEase(Ease.OutCubic);
        }

       /// <summary>
       /// Nhap nhay
       /// </summary>
       /// <param name="target"></param>
       /// <param name="scale"></param>
       /// <param name="duration"></param>
       /// <returns></returns>
        public  Tween Pulse(Transform target, float scale = 1.2f, float duration = 0.5f)
        {
            return target.DOScale(scale, duration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }



        public  Sequence PopupPunch(Transform target)
        {
            Sequence seq = DOTween.Sequence();

            target.localScale = Vector3.zero;

            seq.Append(target.DOScale(1.1f, 0.25f).SetEase(Ease.OutBack));
            seq.Append(target.DOScale(1f, 0.1f));
            seq.Join(target.DOPunchScale(Vector3.one * 0.2f, 0.2f, 5, 1));

            return seq;
        }

        public  Tween RotateLoop(Transform target, Vector3 rotation, float duration = 1f)
        {
            return target.DORotate(rotation, duration, RotateMode.FastBeyond360)
                .SetLoops(-1, LoopType.Incremental)
                .SetEase(Ease.Linear);
        }
    }
}
