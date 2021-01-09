import React, { Component } from 'react';

export class Home extends Component {
    static displayName = Home.name;

    render() {
        return (
            <div>
                <h1>Cache WebSample</h1>
                <p>This is based on REACT Asp.Net Core template. Go to Fetch Data page to see cached data genereated on server for 15 seconds</p>
                <ul>
                    <li>The data is retrived from cache on server not on client</li>
                    <li>The data is always fetched on server even when is a cache hit</li>
                </ul>
                <p>To work properly, you have to set up:</p>
                <ul>
                    <li><strong>ConnectionString</strong>. Located at <code>appsettings.json</code> in root of the websample project.</li>
                    <li><strong>Database User</strong>. The defalut user and password are in the same spot ConnectionString is</li>
                    <li><strong>Npm + Node environment</strong>. This sample uses React so its dependencies comes from Node Land (NPM)</li>
                </ul>
                <p>On the <code>ClientApp</code> open a command prompt in that directory, you can run <code>npm</code> commands such as <code>npm test</code> or <code>npm install</code>.</p>
            </div>
        );
    }
}
