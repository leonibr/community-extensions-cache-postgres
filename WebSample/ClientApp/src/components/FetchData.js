import React, { Component } from 'react';
import './FetchData.css';

export class FetchData extends Component {
    static displayName = FetchData.name;



    constructor(props) {
        super(props);
        this.state = { forecasts: [], loading: true, log: [] };
        this.populateWeatherData = this.populateWeatherData.bind(this);
    }

    componentDidMount() {
       this.populateWeatherData();
    }

    static renderForecastsTable(forecasts) {
        return (
            <table className='table table-striped' aria-labelledby="tabelLabel">
                <thead>
                    <tr>
                        <th>Date</th>
                        <th>Temp. (C)</th>
                        <th>Temp. (F)</th>
                        <th>Summary</th>
                    </tr>
                </thead>
                <tbody>
                    {forecasts.map(forecast =>
                        <tr key={forecast.date}>
                            <td>{forecast.date}</td>
                            <td>{forecast.temperatureC}</td>
                            <td>{forecast.temperatureF}</td>
                            <td>{forecast.summary}</td>
                        </tr>
                    )}
                </tbody>
            </table>
        );
    }

    render() {
        const buttonText = (this.state.loading
            ? <span>Waiting...</span>
            : <span>New Request</span>);
            
        const contents = this.state.loading
            ? <p><em>Loading...</em></p>
            : FetchData.renderForecastsTable(this.state.forecasts);
        const consoleLogger = this.state.loading ? <p></p>
            : <pre className="console">
                {this.state.log.map(s =>
                    <span key={s}>
                        {s}<br />
                    </span>
               )}
              </pre>

        return (
            <div>
                <h1 id="tabelLabel" >Weather forecast with Cache</h1>
                <p>This component demonstrates fetching data CACHED for 15 seconds from the server. </p>
                <p>There is also an intentional 350ms delay on every request to better see the page refresing.</p>
                <button className="btn btn-success" onClick={this.populateWeatherData} disabled={this.state.loading}>
                    {buttonText}
                </button>
                {contents}
                {consoleLogger}
            </div>
        );
    }

    async populateWeatherData() {
        this.setState({ loading: true });
        const dt1 = Date.now();
        const response = await fetch('weatherforecast');
        const time = Date.now() - dt1;
        const data = await response.json();
        const log = this.state.log;
        log.unshift(`  ${JSON.stringify(data)}`);
        log.unshift(`[${new Date().toLocaleTimeString()}] GET ${time}ms`);
        
        this.setState({ forecasts: data, loading: false, log: log });

    }
}
