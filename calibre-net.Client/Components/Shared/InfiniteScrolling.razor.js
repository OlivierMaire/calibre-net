export function initialize(lastIndicator, instance) {
    const options = {
        root: findClosestScrollContainer(lastIndicator),
        rootMargin: '0px',
        threshold: 0,
    };
    setMaxHeight(options.root)

    if (isValidTableElement(lastIndicator.parentElement)) {
        lastIndicator.style.display = 'table-row';
    }
    const observer = new IntersectionObserver(async (entries) => {
        for (const entry of entries) {
            if (entry.isIntersecting) {
                observer.unobserve(lastIndicator);
                await instance.invokeMethodAsync("LoadMoreItems");
            }
        }
    }, options);
    observer.observe(lastIndicator);
    return {
        dispose: () => infiniteScollingDispose(observer),
        onNewItems: () => {
            observer.unobserve(lastIndicator);
            observer.observe(lastIndicator);
        },
        scrollTop: () => scrollTop(options.root)
    };
}
function findClosestScrollContainer(element) {
    while (element) {
        const style = getComputedStyle(element);
        if (style.overflowY !== 'visible') {
            return element;
        }
        element = element.parentElement;
    }
    return null;
}
function infiniteScollingDispose(observer) {
    observer.disconnect();
}
function isValidTableElement(element) {
    if (element === null) {
        return false;
    }
    return ((element instanceof HTMLTableElement && element.style.display === '') || element.style.display === 'table')
        || ((element instanceof HTMLTableSectionElement && element.style.display === '') || element.style.display === 'table-row-group');
}
function setMaxHeight(element) {
    let height = element.offsetTop;
    element.style.maxHeight = `calc(100svh - ${height}px - 48px)`;
}
function scrollTop(element) {
    element.scrollTop = 0;
}