/* Smart On-Call Rota – site.js */
(function(){
  "use strict";
  document.addEventListener("DOMContentLoaded", function(){
    const sidebar     = document.getElementById("sidebar");
    const mainWrapper = document.getElementById("mainWrapper");
    const topbar      = document.getElementById("topbar");
    const toggleBtn   = document.getElementById("sidebarToggle");

    const COLLAPSED = "sidebarCollapsed";
    const isDesktop = () => window.innerWidth >= 992;

    function collapse(){ sidebar?.classList.add("collapsed"); mainWrapper?.classList.add("expanded"); topbar?.classList.add("expanded"); localStorage.setItem(COLLAPSED,"1"); }
    function expand(){   sidebar?.classList.remove("collapsed"); mainWrapper?.classList.remove("expanded"); topbar?.classList.remove("expanded"); localStorage.removeItem(COLLAPSED); }

    // Restore saved sidebar state
    if(isDesktop() && localStorage.getItem(COLLAPSED)) collapse();

    toggleBtn?.addEventListener("click", function(){
      if(isDesktop()){
        sidebar?.classList.contains("collapsed") ? expand() : collapse();
      } else {
        sidebar?.classList.toggle("mobile-open");
      }
    });

    // Close mobile sidebar when clicking outside
    document.addEventListener("click", function(e){
      if(!isDesktop() && sidebar?.classList.contains("mobile-open")){
        if(!sidebar.contains(e.target) && e.target !== toggleBtn){
          sidebar.classList.remove("mobile-open");
        }
      }
    });

    window.addEventListener("resize", function(){
      if(isDesktop()) sidebar?.classList.remove("mobile-open");
    });

    // Live clock in topbar
    const clk = document.getElementById("topClock");
    if(clk){
      const tick = () => {
        clk.textContent = new Date().toLocaleString("en-GB",{
          weekday:"short",day:"2-digit",month:"short",
          hour:"2-digit",minute:"2-digit"
        });
      };
      tick(); setInterval(tick, 30000);
    }

    // Auto-dismiss alerts after 4 s
    document.querySelectorAll(".alert-auto").forEach(function(el){
      setTimeout(function(){
        el.classList.remove("show");
        setTimeout(function(){ el.style.display = "none"; }, 300);
      }, 4000);
    });

    // Confirm-before-submit for data-confirm buttons
    document.querySelectorAll("[data-confirm]").forEach(function(btn){
      btn.addEventListener("click", function(e){
        if(!confirm(this.dataset.confirm || "Are you sure?")){ e.preventDefault(); }
      });
    });

    // DataTables init
    if(typeof $ !== "undefined" && $.fn && $.fn.DataTable){
      $(".datatable").DataTable({
        responsive: true,
        pageLength: 10,
        language: { search: "<i class='bi bi-search me-1'></i>Filter:" }
      });
    }

    // Helper: get CSRF token value from the hidden input
    function getCsrfToken(){
      return document.querySelector("input[name=__RequestVerificationToken]")?.value || "";
    }

    // Mark individual notification read via fetch
    document.querySelectorAll(".mark-read").forEach(function(btn){
      btn.addEventListener("click", async function(){
        const id = this.dataset.id;
        await fetch("/Notifications/MarkRead/" + id, {
          method: "POST",
          headers: { "RequestVerificationToken": getCsrfToken() }
        });
        const row = document.getElementById("notif-" + id);
        const stat = document.getElementById("status-" + id);
        if(row){ row.classList.remove("fw-semibold"); row.classList.add("text-muted"); }
        if(stat){ stat.className = "badge-status badge-active"; stat.textContent = "Sent"; }
        this.textContent = "Read";
        this.disabled = true;
      });
    });
  });
})();
