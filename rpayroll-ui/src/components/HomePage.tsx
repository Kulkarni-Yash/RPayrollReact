import HomeLayout from '../layout/HomeLayout'

type HomePageProps = {
  token: string
  username: string
  onLogout: () => void
}

export default function HomePage({ token, username, onLogout }: HomePageProps) {
  return <HomeLayout token={token} username={username} onLogout={onLogout} />
}
