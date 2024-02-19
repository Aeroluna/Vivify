using System;
using System.Collections;
using System.Collections.Generic;
using CustomJSONData.CustomBeatmap;
using Heck.Animation;
using UnityEngine;
using Vivify.Managers;

namespace Vivify.Events
{
    internal partial class EventController
    {
        internal void SetAnimatorProperty(CustomEventData customEventData)
        {
            if (!_deserializedData.Resolve(customEventData, out SetAnimatorPropertyData? data))
            {
                return;
            }

            float duration = data.Duration;
            duration = 60f * duration / _bpmController.currentBpm; // Convert to real time;

            if (!_prefabManager.TryGetPrefab(data.Id, out InstantiatedPrefab? instantiatedPrefab))
            {
                return;
            }

            List<AnimatorProperty> properties = data.Properties;
            SetAnimatorProperties(instantiatedPrefab.Animators, properties, duration, data.Easing, customEventData.time);
        }

        internal void SetAnimatorProperties(Animator[] animators, List<AnimatorProperty> properties, float duration, Functions easing, float startTime)
        {
            foreach (AnimatorProperty property in properties)
            {
                string name = property.Name;
                AnimatorPropertyType type = property.Type;
                object value = property.Value;
                bool noDuration = duration == 0 || startTime + duration < _audioTimeSource.songTime;
                AnimatedAnimatorProperty? animated = property as AnimatedAnimatorProperty;
                switch (type)
                {
                    case AnimatorPropertyType.Bool:
                        if (animated != null)
                        {
                            if (noDuration)
                            {
                                foreach (Animator animator in animators)
                                {
                                    animator.SetBool(name, animated.PointDefinition.Interpolate(1) >= 1);
                                }
                            }
                            else
                            {
                                StartCoroutine(animated.PointDefinition, animators, name, AnimatorPropertyType.Bool, duration, startTime, easing);
                            }
                        }
                        else
                        {
                            foreach (Animator animator in animators)
                            {
                                animator.SetBool(name, (bool)value);
                            }
                        }

                        break;

                    case AnimatorPropertyType.Float:
                        if (animated != null)
                        {
                            if (noDuration)
                            {
                                foreach (Animator animator in animators)
                                {
                                    animator.SetFloat(name, animated.PointDefinition.Interpolate(1));
                                }
                            }
                            else
                            {
                                StartCoroutine(animated.PointDefinition, animators, name, AnimatorPropertyType.Float, duration, startTime, easing);
                            }
                        }
                        else
                        {
                            foreach (Animator animator in animators)
                            {
                                animator.SetFloat(name, Convert.ToSingle(value));
                            }
                        }

                        break;

                    case AnimatorPropertyType.Integer:
                        if (animated != null)
                        {
                            if (noDuration)
                            {
                                foreach (Animator animator in animators)
                                {
                                    animator.SetFloat(name, animated.PointDefinition.Interpolate(1));
                                }
                            }
                            else
                            {
                                StartCoroutine(animated.PointDefinition, animators, name, AnimatorPropertyType.Float, duration, startTime, easing);
                            }
                        }
                        else
                        {
                            foreach (Animator animator in animators)
                            {
                                animator.SetFloat(name, Convert.ToSingle(value));
                            }
                        }

                        break;

                    case AnimatorPropertyType.Trigger:
                        bool trigger = (bool)value;
                        foreach (Animator animator in animators)
                        {
                            if (trigger)
                            {
                                animator.SetTrigger(name);
                            }
                            else
                            {
                                animator.ResetTrigger(name);
                            }
                        }

                        break;

                    default:
                        _log.Error($"[{type}] invalid");
                        break;
                }
            }
        }

        private void StartCoroutine(
            PointDefinition<float> points,
            Animator[] animators,
            string name,
            AnimatorPropertyType type,
            float duration,
            float startTime,
            Functions easing)
            => _coroutineDummy.StartCoroutine(AnimatePropertyCoroutine(points, animators, name, type, duration, startTime, easing));

        private IEnumerator AnimatePropertyCoroutine(PointDefinition<float> points, Animator[] animators, string name, AnimatorPropertyType type, float duration, float startTime, Functions easing)
        {
            while (true)
            {
                float elapsedTime = _audioTimeSource.songTime - startTime;

                if (elapsedTime < duration)
                {
                    float time = Easings.Interpolate(Mathf.Min(elapsedTime / duration, 1f), easing);
                    switch (type)
                    {
                        case AnimatorPropertyType.Bool:
                            {
                                bool value = points.Interpolate(time) >= 1;
                                foreach (Animator animator in animators)
                                {
                                    animator.SetBool(name, value);
                                }
                            }

                            break;

                        case AnimatorPropertyType.Float:
                            {
                                float value = points.Interpolate(time);
                                foreach (Animator animator in animators)
                                {
                                    animator.SetFloat(name, value);
                                }
                            }

                            break;

                        case AnimatorPropertyType.Integer:
                            {
                                float value = points.Interpolate(time);
                                foreach (Animator animator in animators)
                                {
                                    animator.SetInteger(name, (int)value);
                                }
                            }

                            break;

                        default:
                            yield break;
                    }

                    yield return null;
                }
                else
                {
                    break;
                }
            }
        }
    }
}
