/**
 * DOM Ready Utility Function
 * Ensures callback executes only after DOM is fully loaded
 * Handles both immediate ready state and pending ready state scenarios
 * Uses DOMContentLoaded event for modern browser compatibility
 */
function onDOMReady(callback) {
  // Check if callback is a function
  if (typeof callback !== "function") {
    console.warn("onDOMReady: callback must be a function");
    return;
  }

  // If DOM is already loaded, execute callback immediately
  if (
    document.readyState === "complete" ||
    document.readyState === "interactive"
  ) {
    // Use setTimeout to ensure callback runs after current execution stack
    setTimeout(callback, 0);
    return;
  }

  // DOM is still loading, wait for DOMContentLoaded event
  document.addEventListener("DOMContentLoaded", callback, { once: true });
}

// Initialize sidebar functionality when DOM is ready
onDOMReady(function () {
  const burger = document.getElementById("burger");
  const sidebar = document.getElementById("sidebar");

  // Exit early if required elements are not found
  if (!burger || !sidebar) {
    console.warn(
      "Sidebar elements not found. Burger or sidebar element is missing."
    );
    return;
  }

  let wasSidebarOpenBeforeResize = true; // Assume initially open

  burger.addEventListener("click", () => {
    const isCollapsed = sidebar.classList.toggle("collapsed");
    burger.classList.toggle("closed", isCollapsed);
  });

  const checkResize = () => {
    const isSmallScreen = window.innerWidth < 768;

    if (isSmallScreen) {
      wasSidebarOpenBeforeResize = !sidebar.classList.contains("collapsed");
      sidebar.classList.add("collapsed");
      burger.classList.add("closed");
    } else {
      if (wasSidebarOpenBeforeResize) {
        sidebar.classList.remove("collapsed");
        burger.classList.remove("closed");
      } else {
        sidebar.classList.add("collapsed");
        burger.classList.add("closed");
      }
    }
  };

  let resizeTimeout;
  window.addEventListener("resize", () => {
    clearTimeout(resizeTimeout);
    resizeTimeout = setTimeout(checkResize, 150);
  });

  // Initial check on page load
  checkResize();

  // Handle sidebar navigation
  const sidebarItems = sidebar.querySelectorAll("li[data-url]");
  
  sidebarItems.forEach((item) => {
    item.addEventListener("click", function () {
      const url = this.getAttribute("data-url");
      
      if (url) {
        window.location.href = url;
      } else {
        console.warn("No data-url attribute found on sidebar item");
      }
    });
  });
});
