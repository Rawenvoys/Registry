// Opens the provider's sign-in page in a popup and resolves with the URL fragment
// that signin-callback.html sends back, or null when the popup could not be opened.
// A BroadcastChannel is used instead of window.opener, which providers' COOP headers cut off.
export function signIn(url) {
    return new Promise(resolve => {
        const channel = new BroadcastChannel('registry-signin');
        const popup = window.open(url, 'registry-signin', 'width=500,height=650');
        if (!popup) {
            channel.close();
            resolve(null);
            return;
        }

        channel.onmessage = event => {
            channel.close();
            resolve(typeof event.data === 'string' ? event.data : null);
        };
    });
}
