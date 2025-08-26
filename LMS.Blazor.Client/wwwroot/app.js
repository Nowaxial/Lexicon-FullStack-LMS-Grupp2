window.closeBootstrapModal = (id) => {
    var modalElement = document.getElementById(id);
    if (modalElement) {
        var modal = bootstrap.Modal.getInstance(modalElement);
        if (modal) {
            modal.hide();
        }
    }
};