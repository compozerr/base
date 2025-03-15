type Link = { "dev": string, "prod": string };

const Links = {
    documentation: {
        "dev": "http://localhost:1236",
        "prod": "https://docs.compozerr.com"
    },
    addNewService: {
        "dev": "http://localhost:1236/basics/deploy-your-project",
        "prod": "https://docs.compozerr.com/basics/deploy-your-project"
    }
} satisfies { [key: string]: Link };

type AvailableLinks = keyof typeof Links;  // This will be exactly "documentation"

export const getLink = (link: AvailableLinks) => {
    const isDev = process.env.NODE_ENV === 'development';

    return isDev ? Links[link].dev : Links[link].prod;
}
