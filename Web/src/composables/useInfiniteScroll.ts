import { nextTick, onMounted, onUnmounted, watch, type Ref } from 'vue'

/** 触底加载：IntersectionObserver + 加载后补检 + window scroll 兜底 */
export function useInfiniteScroll(
  sentinel: Ref<HTMLElement | null>,
  onLoadMore: () => void,
  options?: { rootMargin?: string },
) {
  const margin = options?.rootMargin ?? '480px 0px 480px 0px'
  let observer: IntersectionObserver | null = null
  let scrollRaf = 0

  function isNearViewport(): boolean {
    if (!sentinel.value) return false
    const rect = sentinel.value.getBoundingClientRect()
    return rect.top <= window.innerHeight + 480
  }

  function onScroll() {
    if (scrollRaf) return
    scrollRaf = requestAnimationFrame(() => {
      scrollRaf = 0
      if (isNearViewport()) onLoadMore()
    })
  }

  /** 加载一批后若哨兵仍在视口内，继续加载（修复 IO 不重复触发） */
  async function checkAfterLoad() {
    await nextTick()
    requestAnimationFrame(onScroll)
  }

  onMounted(() => {
    observer = new IntersectionObserver(
      (entries) => {
        if (entries.some((e) => e.isIntersecting)) onLoadMore()
      },
      { rootMargin: margin, threshold: 0 },
    )

    watch(
      sentinel,
      (el, _, onCleanup) => {
        if (!el || !observer) return
        observer.observe(el)
        onCleanup(() => observer?.unobserve(el))
      },
      { flush: 'post' },
    )

    window.addEventListener('scroll', onScroll, { passive: true })
    window.addEventListener('resize', onScroll, { passive: true })
  })

  onUnmounted(() => {
    observer?.disconnect()
    window.removeEventListener('scroll', onScroll)
    window.removeEventListener('resize', onScroll)
    if (scrollRaf) cancelAnimationFrame(scrollRaf)
  })

  return { checkAfterLoad }
}
