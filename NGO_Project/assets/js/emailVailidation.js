$(document).ready(function () {
    const email = $('[name="Email"]').val();
    const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (email && !emailPattern.test(email)) {
        $('[name="Email"]').addClass('is-invalid');
        isValid = false;
    }

    const password = $('#registerPassword').val();
    const strongPasswordPattern = /^(?=.*[A-Za-z])(?=.*\d)[A-Za-z\d@$!%*?&]{8,}$/;
    if (!strongPasswordPattern.test(password)) {
        $('#registerPassword').addClass('is-invalid');
        isValid = false;
    }
});