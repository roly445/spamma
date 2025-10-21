module.exports = {
    plugins: {
        '@tailwindcss/postcss': {
            base: `${__dirname}/../..`,
            optimize: true,
        }
    }
}