// Add to site.js
$(document).ready(function () {
    $('#sidebarCollapse').on('click', function () {
        $('#sidebar').toggleClass('active');
        $('#content').toggleClass('active');
    });
});

$(() => {
    $(register).on('click', async (e) => {
        $(register).text('Please wait...').attr('disabled', true)
        const _response = await fetch("/register-urls")
        const response = await _response.json();

        $('#feedback').html(JSON.stringify(response))

        console.log(response)
        $(register).text('Register Url').attr('disabled')
    });

})