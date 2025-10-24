const path = require('path');
const MiniCssExtractPlugin = require("mini-css-extract-plugin");
const CopyPlugin = require("copy-webpack-plugin");

module.exports = {
    entry: {
        app: ['./Assets/Styles/app.scss', './Assets/Scripts/app.ts'],
        'setup-keys': ['./Assets/Scripts/setup-keys.ts'],
        'setup-email': ['./Assets/Scripts/setup-email.ts'],
        'setup-hosting': ['./Assets/Scripts/setup-hosting.ts'],
        'setup-admin': ['./Assets/Scripts/setup-admin.ts']
    },
    output: {
        path: path.join(__dirname, 'wwwroot/'),
        publicPath: '/',
        filename: '[name].js'
    },
    plugins: [
        new MiniCssExtractPlugin({
            filename: '[name].css'
        }),
        new CopyPlugin({
            patterns: [
                { from: "./Assets/images/*", to: path.join(__dirname, 'wwwroot/[name][ext]') }
            ],
        }),
    ],
    module: {
        rules: [
            {
                test: /\.ts?$/,
                use: 'ts-loader',
                exclude: /node_modules/,
                generator: {
                    filename: 'abc.js'
                }
            },
            {
                test: /\.(css|sass|scss)$/,
                use: [
                    MiniCssExtractPlugin.loader,
                    'css-loader',
                    {
                        loader: "postcss-loader",
                    }
                ]
            },
            {
                // To use images on pug files:
                test: /\.(png|jpg|jpeg|ico)/,
                type: 'asset/resource',
                generator: {
                    filename: '[name][ext]'
                }
            },
            {
                // To use fonts on pug files:
                test: /\.(woff|woff2|eot|ttf|otf|svg)$/i,
                type: 'asset/resource',
                generator: {
                    filename: '[name][ext][query]'
                }
            },
            {
                // To use config:
                test: /\.(json)$/i,
                type: 'asset/resource',
                generator: {
                    filename: '[name][ext]'
                }
            }
        ]
    },
    devtool: 'inline-source-map',
    stats: 'errors-only',
    resolve: {
        extensions: ['.tsx', '.ts', '.js'],
    }
};