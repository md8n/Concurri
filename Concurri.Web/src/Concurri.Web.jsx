import React from 'react';
import $ from 'jquery';
window.jQuery = $;
var dotnetify = require('dotnetify');

class Concurri_Web extends React.Component {
    constructor(props) {
        super(props);
        window.jQuery = $;
        dotnetify.react.connect("Concurri_Web", this);
        this.state = { Greetings: "", ServerTime: "" };
    }
    render() {
        return (
            <div>
                {this.state.Greetings}<br />
                Server time is: {this.state.ServerTime}
            </div>
        );
    }
}
export default HelloWorld;